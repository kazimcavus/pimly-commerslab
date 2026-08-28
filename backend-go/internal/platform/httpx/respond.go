package httpx

import (
	"encoding/json"
	"errors"
	"io"
	"net/http"
	"strconv"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// WriteResult, değersiz bir işlem sonucunu HTTP yanıtına çevirir:
// başarı → 204 No Content, hata → ProblemDetails
// (.NET ResultExtensions.ToHttpResult karşılığı).
func WriteResult(w http.ResponseWriter, r *http.Request, result sharedkernel.Result) {
	if result.IsFailure() {
		WriteProblem(w, r, result.Err())
		return
	}
	w.WriteHeader(http.StatusNoContent)
}

// WriteOK, değer taşıyan başarılı sonucu 200 OK + JSON gövdeyle yazar;
// hata durumunda ProblemDetails üretir.
func WriteOK[T any](w http.ResponseWriter, r *http.Request, result sharedkernel.ResultOf[T]) {
	if result.IsFailure() {
		WriteProblem(w, r, result.Err())
		return
	}
	writeJSON(w, http.StatusOK, "application/json; charset=utf-8", result.Value())
}

// WriteCreated, başarılı sonucu 201 Created + Location başlığıyla yazar;
// location, oluşturulan kaynağın URL'sini değerden üretir
// (.NET ResultExtensions.ToCreatedResult karşılığı).
func WriteCreated[T any](w http.ResponseWriter, r *http.Request, result sharedkernel.ResultOf[T], location func(T) string) {
	if result.IsFailure() {
		WriteProblem(w, r, result.Err())
		return
	}
	w.Header().Set("Location", location(result.Value()))
	writeJSON(w, http.StatusCreated, "application/json; charset=utf-8", result.Value())
}

// WriteAccepted, kuyruklanmış bir işi 202 Accepted + JSON gövdeyle yazar;
// location boş değilse Location başlığı eklenir.
func WriteAccepted[T any](w http.ResponseWriter, r *http.Request, result sharedkernel.ResultOf[T], location string) {
	if result.IsFailure() {
		WriteProblem(w, r, result.Err())
		return
	}
	if location != "" {
		w.Header().Set("Location", location)
	}
	writeJSON(w, http.StatusAccepted, "application/json; charset=utf-8", result.Value())
}

// DecodeJSON, istek gövdesini hedef türe çözer. Bozuk veya boş gövde,
// .NET model bağlamanın ürettiği gibi 400'e eşlenecek bir doğrulama hatası döner.
func DecodeJSON[T any](r *http.Request) (T, *sharedkernel.Error) {
	var target T
	if err := json.NewDecoder(r.Body).Decode(&target); err != nil {
		if errors.Is(err, io.EOF) {
			return target, sharedkernel.NewValidationError("Request body is required.")
		}
		return target, sharedkernel.NewValidationError("Request body is not valid JSON.")
	}
	return target, nil
}

// QueryPagination, page ve page_size sorgu parametrelerini çözer.
// Parametre yokken veya 0 gönderildiğinde varsayılanlar kullanılır
// (.NET endpoint'lerinde parametrelerin 0 varsayılanlı olması + Resolve davranışı);
// sayı olmayan değer doğrulama hatası döner.
func QueryPagination(r *http.Request) sharedkernel.ResultOf[sharedkernel.Pagination] {
	page, perr := queryInt(r, "page")
	if perr != nil {
		return sharedkernel.FailOf[sharedkernel.Pagination](perr)
	}
	size, serr := queryInt(r, "page_size")
	if serr != nil {
		return sharedkernel.FailOf[sharedkernel.Pagination](serr)
	}
	return sharedkernel.ResolvePagination(page, size)
}

// queryInt, tek bir tamsayı sorgu parametresini okur; yoksa 0 döner.
func queryInt(r *http.Request, name string) (int, *sharedkernel.Error) {
	raw := r.URL.Query().Get(name)
	if raw == "" {
		return 0, nil
	}
	n, err := strconv.Atoi(raw)
	if err != nil {
		return 0, sharedkernel.NewValidationError("Query parameter '" + name + "' must be an integer.")
	}
	return n, nil
}
