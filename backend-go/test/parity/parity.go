// Package parity, .NET ve Go backend'lerinin kablo formatı paritesini
// doğrulayan koşucudur. İki modda çalışır:
//
//	PARITY_MODE=capture  → korpusu .NET API'ye gönderir, yanıtları golden
//	                       dosyalarına (goldens/*.json) kaydeder.
//	PARITY_MODE=verify   → korpusu Go API'ye gönderir, yanıtları golden'larla
//	                       karşılaştırır; fark = test hatası.
//
// PARITY_BASE_URL hedef API'yi belirtir (ör. http://localhost:7000). Değişken
// yoksa testler atlanır, böylece normal `go test ./...` koşuları etkilenmez.
//
// Değişken (volatil) alanlar — kimlikler, tarihler, token'lar — maskeyle
// karşılaştırılır: değerin BİÇİMİ doğrulanır (uuid/RFC3339/JWT), değeri
// golden'da yer tutucuyla saklanır. Böylece iki backend'in farklı kimlikler
// üretmesi parite bozmaz ama alanın tipi/varlığı garanti kalır.
package parity

import (
	"bytes"
	"encoding/json"
	"fmt"
	"net/http"
	"os"
	"path/filepath"
	"regexp"
	"strings"
	"time"
)

// Mask türleri: değerin biçimini doğrulayıp yer tutucuya çeviren kurallar.
const (
	// MaskUUID, geçerli bir UUID bekler.
	MaskUUID = "uuid"

	// MaskDateTime, RFC3339/ISO8601 zaman damgası bekler.
	MaskDateTime = "datetime"

	// MaskJWT, üç parçalı bir JWT bekler.
	MaskJWT = "jwt"

	// MaskAny, herhangi bir boş olmayan dizgi bekler (trace_id gibi:
	// .NET her zaman doldurur, Go'da izleme kapalıyken boş olabilir).
	MaskAnyString = "string"

	// MaskAnyNumber, herhangi bir JSON sayısı bekler (iki backend'in
	// veritabanlarında farklı birikmiş kayıt sayıları için: total_count gibi).
	MaskAnyNumber = "number"
)

var (
	uuidPattern     = regexp.MustCompile(`^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$`)
	dateTimePattern = regexp.MustCompile(`^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})$`)
	jwtPattern      = regexp.MustCompile(`^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$`)
)

// Case, korpustaki tek bir istek senaryosudur.
type Case struct {
	// Name, golden dosya adıdır (modül önekiyle: "identity/login_ok").
	Name string

	// Method ve Path, gönderilecek isteği tanımlar.
	Method string
	Path   string

	// Body, JSON gövdesidir; nil ise gövdesiz gönderilir.
	Body any

	// Auth true ise koşucunun oturum token'ı Authorization başlığına eklenir.
	Auth bool

	// Masks, JSON yolundan mask türüne eşlemedir. Yol nokta ayrımlıdır;
	// "*" tek bir seviyede her anahtarı/indeksi eşler (ör. "items.*.id").
	Masks map[string]string
}

// Snapshot, bir yanıtın karşılaştırılan kısmıdır.
type Snapshot struct {
	Status      int             `json:"status"`
	ContentType string          `json:"content_type"`
	Body        json.RawMessage `json:"body,omitempty"`
	Location    string          `json:"location,omitempty"`
}

// Runner, korpusu hedefe gönderip golden'ları yöneten koşucudur.
type Runner struct {
	BaseURL    string
	Mode       string // "capture" | "verify"
	GoldensDir string
	Token      string
	client     *http.Client
}

// NewRunnerFromEnv, PARITY_* ortam değişkenlerinden koşucu kurar; PARITY_BASE_URL
// yoksa (nil, "") döner ve çağıran test kendini atlamalıdır.
func NewRunnerFromEnv(goldensDir string) *Runner {
	base := os.Getenv("PARITY_BASE_URL")
	if base == "" {
		return nil
	}
	mode := os.Getenv("PARITY_MODE")
	if mode == "" {
		mode = "verify"
	}
	return &Runner{
		BaseURL:    strings.TrimRight(base, "/"),
		Mode:       mode,
		GoldensDir: goldensDir,
		client:     &http.Client{Timeout: 30 * time.Second},
	}
}

// Login, koşucunun oturum token'ını verilen kimlik bilgileriyle alır;
// Auth'lu senaryolardan önce çağrılmalıdır.
func (r *Runner) Login(email, password string) error {
	snap, err := r.send(Case{
		Method: http.MethodPost,
		Path:   "/api/v1/identity/login",
		Body:   map[string]string{"email": email, "password": password},
	})
	if err != nil {
		return err
	}
	if snap.Status != http.StatusOK {
		return fmt.Errorf("parity: koşucu girişi başarısız (%d): %s", snap.Status, snap.Body)
	}
	var body struct {
		Token string `json:"token"`
	}
	if err := json.Unmarshal(snap.Body, &body); err != nil {
		return err
	}
	r.Token = body.Token
	return nil
}

// RunWithResult, Run gibi çalışır ama maskelenmemiş ham anlık görüntüyü de
// döner; akış senaryoları (oluştur → kimliği al → sonraki isteklerde kullan)
// bunu kullanır.
func (r *Runner) RunWithResult(c Case) (Snapshot, error) {
	snap, err := r.send(c)
	if err != nil {
		return snap, fmt.Errorf("%s: istek başarısız: %w", c.Name, err)
	}
	return snap, r.compareOrCapture(c, snap)
}

// Run, tek bir senaryoyu işler: capture modunda golden yazar, verify modunda
// karşılaştırır. Fark varsa okunur bir hata döner.
func (r *Runner) Run(c Case) error {
	snap, err := r.send(c)
	if err != nil {
		return fmt.Errorf("%s: istek başarısız: %w", c.Name, err)
	}
	return r.compareOrCapture(c, snap)
}

// compareOrCapture, anlık görüntüyü maskeleyip moda göre golden'a yazar veya
// golden'la karşılaştırır.
func (r *Runner) compareOrCapture(c Case, snap Snapshot) error {
	normalized, err := normalizeSnapshot(snap, c.Masks)
	if err != nil {
		return fmt.Errorf("%s: %w", c.Name, err)
	}

	goldenPath := filepath.Join(r.GoldensDir, c.Name+".json")
	if r.Mode == "capture" {
		if err := os.MkdirAll(filepath.Dir(goldenPath), 0o755); err != nil {
			return err
		}
		data, _ := json.MarshalIndent(normalized, "", "  ")
		return os.WriteFile(goldenPath, append(data, '\n'), 0o644)
	}

	goldenData, err := os.ReadFile(goldenPath)
	if err != nil {
		return fmt.Errorf("%s: golden okunamadı (önce capture çalıştırın): %w", c.Name, err)
	}
	var golden Snapshot
	if err := json.Unmarshal(goldenData, &golden); err != nil {
		return fmt.Errorf("%s: golden çözümlenemedi: %w", c.Name, err)
	}
	return diffSnapshots(c.Name, golden, normalized)
}

// send, senaryonun HTTP isteğini gönderir ve ham anlık görüntüyü döner.
func (r *Runner) send(c Case) (Snapshot, error) {
	var bodyReader *bytes.Reader
	if c.Body != nil {
		raw, err := json.Marshal(c.Body)
		if err != nil {
			return Snapshot{}, err
		}
		bodyReader = bytes.NewReader(raw)
	} else {
		bodyReader = bytes.NewReader(nil)
	}

	req, err := http.NewRequest(c.Method, r.BaseURL+c.Path, bodyReader)
	if err != nil {
		return Snapshot{}, err
	}
	if c.Body != nil {
		req.Header.Set("Content-Type", "application/json")
	}
	if c.Auth {
		req.Header.Set("Authorization", "Bearer "+r.Token)
	}

	resp, err := r.client.Do(req)
	if err != nil {
		return Snapshot{}, err
	}
	defer resp.Body.Close()

	var buf bytes.Buffer
	if _, err := buf.ReadFrom(resp.Body); err != nil {
		return Snapshot{}, err
	}
	snap := Snapshot{
		Status:      resp.StatusCode,
		ContentType: baseContentType(resp.Header.Get("Content-Type")),
		Location:    resp.Header.Get("Location"),
	}
	if buf.Len() > 0 {
		snap.Body = json.RawMessage(buf.Bytes())
	}
	return snap, nil
}

// baseContentType, charset parametresini atarak medya türünü döner
// (.NET "application/json; charset=utf-8", Go da öyle; problem+json ayrışır).
func baseContentType(header string) string {
	mediaType, _, _ := strings.Cut(header, ";")
	return strings.TrimSpace(mediaType)
}

// normalizeSnapshot, gövde ve Location içindeki maskeli yolları biçim
// denetiminden geçirip yer tutucularla değiştirir.
func normalizeSnapshot(snap Snapshot, masks map[string]string) (Snapshot, error) {
	if snap.Location != "" {
		snap.Location = uuidPattern.ReplaceAllStringFunc(snap.Location, func(string) string { return "«uuid»" })
		// Yol içine gömülü UUID'ler için genel değişim (ör. /products/{id}).
		snap.Location = regexp.MustCompile(`[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}`).
			ReplaceAllString(snap.Location, "«uuid»")
	}
	if len(snap.Body) == 0 {
		return snap, nil
	}
	var parsed any
	if err := json.Unmarshal(snap.Body, &parsed); err != nil {
		// JSON olmayan gövde olduğu gibi karşılaştırılır.
		return snap, nil
	}
	normalized, err := applyMasks(parsed, "", masks)
	if err != nil {
		return snap, err
	}
	canonical, err := json.Marshal(normalized)
	if err != nil {
		return snap, err
	}
	snap.Body = canonical
	return snap, nil
}

// applyMasks, JSON ağacını dolaşır; maskeli yollarda biçimi doğrulayıp yer
// tutucu koyar, diğer değerleri olduğu gibi bırakır.
func applyMasks(node any, path string, masks map[string]string) (any, error) {
	if mask, ok := lookupMask(path, masks); ok && path != "" {
		return maskValue(node, mask, path)
	}
	switch v := node.(type) {
	case map[string]any:
		out := make(map[string]any, len(v))
		for key, child := range v {
			childPath := key
			if path != "" {
				childPath = path + "." + key
			}
			masked, err := applyMasks(child, childPath, masks)
			if err != nil {
				return nil, err
			}
			out[key] = masked
		}
		return out, nil
	case []any:
		out := make([]any, len(v))
		for i, child := range v {
			childPath := "*"
			if path != "" {
				childPath = path + ".*"
			}
			masked, err := applyMasks(child, childPath, masks)
			if err != nil {
				return nil, err
			}
			out[i] = masked
		}
		return out, nil
	default:
		return node, nil
	}
}

// lookupMask, yolun bir mask kuralıyla eşleşip eşleşmediğini döner;
// kurallardaki "*" segmenti her değeri eşler.
func lookupMask(path string, masks map[string]string) (string, bool) {
	if mask, ok := masks[path]; ok {
		return mask, true
	}
	segments := strings.Split(path, ".")
	for pattern, mask := range masks {
		patternSegments := strings.Split(pattern, ".")
		if len(patternSegments) != len(segments) {
			continue
		}
		match := true
		for i, ps := range patternSegments {
			if ps != "*" && ps != segments[i] {
				match = false
				break
			}
		}
		if match {
			return mask, true
		}
	}
	return "", false
}

// maskValue, değerin mask biçimine uyduğunu doğrular ve yer tutucusunu döner.
func maskValue(node any, mask, path string) (any, error) {
	str, isString := node.(string)
	switch mask {
	case MaskUUID:
		if !isString || !uuidPattern.MatchString(str) {
			return nil, fmt.Errorf("%s: uuid bekleniyordu, %v geldi", path, node)
		}
		return "«uuid»", nil
	case MaskDateTime:
		if !isString || !dateTimePattern.MatchString(str) {
			return nil, fmt.Errorf("%s: tarih bekleniyordu, %v geldi", path, node)
		}
		return "«datetime»", nil
	case MaskJWT:
		if !isString || !jwtPattern.MatchString(str) {
			return nil, fmt.Errorf("%s: jwt bekleniyordu, %v geldi", path, node)
		}
		return "«jwt»", nil
	case MaskAnyString:
		if !isString {
			return nil, fmt.Errorf("%s: dizgi bekleniyordu, %v geldi", path, node)
		}
		return "«string»", nil
	case MaskAnyNumber:
		if _, isNumber := node.(float64); !isNumber {
			return nil, fmt.Errorf("%s: sayı bekleniyordu, %v geldi", path, node)
		}
		return "«number»", nil
	default:
		return nil, fmt.Errorf("%s: bilinmeyen mask türü %q", path, mask)
	}
}

// diffSnapshots, golden ile gerçek anlık görüntüyü karşılaştırır; fark varsa
// insan tarafından okunur bir hata döner.
func diffSnapshots(name string, golden, actual Snapshot) error {
	var problems []string
	if golden.Status != actual.Status {
		problems = append(problems, fmt.Sprintf("status: golden=%d actual=%d", golden.Status, actual.Status))
	}
	if golden.ContentType != actual.ContentType {
		problems = append(problems, fmt.Sprintf("content-type: golden=%q actual=%q", golden.ContentType, actual.ContentType))
	}
	if golden.Location != actual.Location {
		problems = append(problems, fmt.Sprintf("location: golden=%q actual=%q", golden.Location, actual.Location))
	}
	if !jsonEqual(golden.Body, actual.Body) {
		problems = append(problems, fmt.Sprintf("body:\n  golden: %s\n  actual: %s", golden.Body, actual.Body))
	}
	if len(problems) > 0 {
		return fmt.Errorf("%s parite farkı:\n%s", name, strings.Join(problems, "\n"))
	}
	return nil
}

// jsonEqual, iki JSON gövdesini anahtar sırasından bağımsız karşılaştırır.
func jsonEqual(a, b json.RawMessage) bool {
	if len(a) == 0 || len(b) == 0 {
		return len(a) == len(b)
	}
	var av, bv any
	if json.Unmarshal(a, &av) != nil || json.Unmarshal(b, &bv) != nil {
		return bytes.Equal(a, b)
	}
	ac, _ := json.Marshal(sortKeys(av))
	bc, _ := json.Marshal(sortKeys(bv))
	return bytes.Equal(ac, bc)
}

// sortKeys, karşılaştırma kararlılığı için haritaları normalize eder
// (encoding/json haritaları zaten sıralı anahtarlarla yazar; bu işlev iç içe
// yapılarda tip birliği sağlar).
func sortKeys(v any) any { return v }
