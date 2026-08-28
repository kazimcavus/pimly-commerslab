// Package sharedkernel, tüm modüllerin paylaştığı sözleşme ilkellerini içerir:
// Result/Error deseni, doğrulama hataları, sayfalama ve pazaryeri değer nesnesi.
// .NET tarafındaki SharedKernel projesinin birebir Go karşılığıdır; buradaki hata
// ve doğrulama kodları API'nin kablo formatına (ProblemDetails) aynen yansır,
// bu yüzden değerleri asla değiştirilmemelidir.
package sharedkernel

// Üst düzey hata kodları. API ve domain katmanlarında kullanılır; HTTP durum
// koduna eşlenirler (bkz. platform/httpx). Değerler .NET ErrorCodes ile birebir
// aynıdır ve frontend bu dizgilere bağımlıdır.
const (
	// ErrorCodeValidation, istek veya alan doğrulaması başarısız olduğunda kullanılır (HTTP 400).
	ErrorCodeValidation = "validation"

	// ErrorCodeNotFound, istenen kaynak bulunamadığında kullanılır (HTTP 404).
	ErrorCodeNotFound = "not_found"

	// ErrorCodeConflict, iş kuralı çakışması veya geçersiz durum geçişinde kullanılır (HTTP 409).
	ErrorCodeConflict = "conflict"

	// ErrorCodeFailure, genel işlem hatası durumunda kullanılır (HTTP 400).
	ErrorCodeFailure = "failure"

	// ErrorCodeInternal, beklenmeyen sunucu hatası durumunda kullanılır (HTTP 500).
	ErrorCodeInternal = "internal_error"

	// ErrorCodeUnauthorized, kimlik doğrulama veya yetkilendirme başarısız olduğunda kullanılır (HTTP 401).
	ErrorCodeUnauthorized = "unauthorized"
)

// Alan düzeyinde doğrulama hata kodları. ProblemDetails yanıtındaki errors
// sözlüğünde alan başına code olarak döner; .NET ValidationErrorCodes ile birebir aynıdır.
const (
	// ValidationCodeRequired, zorunlu alan boş bırakıldığında kullanılır.
	ValidationCodeRequired = "required"

	// ValidationCodeMaxLength, alan izin verilen azami uzunluğu aştığında kullanılır.
	ValidationCodeMaxLength = "max_length"

	// ValidationCodeInvalidEnum, alan kapalı bir değer kümesinin dışında olduğunda kullanılır.
	ValidationCodeInvalidEnum = "invalid_enum"

	// ValidationCodeInvalidID, alan geçerli bir kimlik (UUID) olmadığında kullanılır.
	ValidationCodeInvalidID = "invalid_id"

	// ValidationCodeInvalidFormat, alan beklenen biçime uymadığında kullanılır.
	ValidationCodeInvalidFormat = "invalid_format"

	// ValidationCodeUnknown, daha özel bir kodla eşlenemeyen doğrulama hatalarında kullanılır.
	ValidationCodeUnknown = "unknown"
)

// ValidationError, belirli bir alan için doğrulama hatasını temsil eder.
// Field hatalı alanın (snake_case) adı, Code alan düzeyinde hata kodu,
// Message insan tarafından okunabilir mesajdır.
type ValidationError struct {
	Field   string
	Code    string
	Message string
}

// Error, hata kodu, mesajı ve isteğe bağlı doğrulama hatalarını taşıyan domain
// hatasıdır. Go'nun error arayüzünü uygular; handler'lar Result üzerinden döndürür,
// HTTP katmanı ProblemDetails'e çevirir. .NET'teki SharedKernel.Error kaydının karşılığıdır.
type Error struct {
	// Code, üst düzey hata kodudur (ErrorCode* sabitlerinden biri).
	Code string

	// Message, insan tarafından okunabilir özet hata mesajıdır.
	Message string

	// ValidationErrors, alan düzeyinde doğrulama hatalarıdır; yalnızca
	// doğrulama hatalarında dolu olur.
	ValidationErrors []ValidationError
}

// Error, Go error arayüzünü uygular; log ve sarmalama senaryoları için
// "kod: mesaj" biçiminde döner.
func (e *Error) Error() string { return e.Code + ": " + e.Message }

// NewValidationError, doğrulama hatası oluşturur; validationErrors boş geçilebilir.
func NewValidationError(message string, validationErrors ...ValidationError) *Error {
	return &Error{Code: ErrorCodeValidation, Message: message, ValidationErrors: validationErrors}
}

// NewNotFoundError, kaynak bulunamadı hatası oluşturur.
func NewNotFoundError(message string) *Error {
	return &Error{Code: ErrorCodeNotFound, Message: message}
}

// NewConflictError, iş kuralı çakışması hatası oluşturur.
func NewConflictError(message string) *Error {
	return &Error{Code: ErrorCodeConflict, Message: message}
}

// NewUnauthorizedError, kimlik doğrulama/yetkilendirme hatası oluşturur.
func NewUnauthorizedError(message string) *Error {
	return &Error{Code: ErrorCodeUnauthorized, Message: message}
}

// NewFailureError, genel işlem hatası oluşturur.
func NewFailureError(message string) *Error {
	return &Error{Code: ErrorCodeFailure, Message: message}
}

// NewInternalError, beklenmeyen sunucu hatası oluşturur.
func NewInternalError(message string) *Error {
	return &Error{Code: ErrorCodeInternal, Message: message}
}
