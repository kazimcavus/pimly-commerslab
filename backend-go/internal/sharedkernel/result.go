package sharedkernel

// Result, değer taşımayan bir işlemin başarı/başarısızlık sonucunu temsil eder.
// .NET'teki SharedKernel.Result sınıfının karşılığıdır: başarısız sonuç mutlaka
// bir *Error taşır, başarılı sonuç taşımaz. Handler'lar bu türü döndürür; HTTP
// katmanı başarıyı 204'e, hatayı ProblemDetails'e çevirir.
type Result struct {
	err *Error
}

// Ok, başarılı (değersiz) sonuç oluşturur.
func Ok() Result { return Result{} }

// Fail, verilen hatayla başarısız sonuç oluşturur; err nil olamaz.
func Fail(err *Error) Result {
	if err == nil {
		panic("sharedkernel: başarısız Result hatasız oluşturulamaz")
	}
	return Result{err: err}
}

// IsSuccess, işlemin başarılı olup olmadığını döner.
func (r Result) IsSuccess() bool { return r.err == nil }

// IsFailure, işlemin başarısız olup olmadığını döner.
func (r Result) IsFailure() bool { return r.err != nil }

// Err, başarısız sonuçtaki hatayı döner; başarılı sonuçta nil'dir.
func (r Result) Err() *Error { return r.err }

// ResultOf, T türünde bir değer taşıyan işlem sonucunu temsil eder.
// .NET'teki SharedKernel.Result<T> sınıfının karşılığıdır: değere yalnızca
// başarı durumunda erişilmelidir.
type ResultOf[T any] struct {
	value T
	err   *Error
}

// OkOf, verilen değerle başarılı sonuç oluşturur.
func OkOf[T any](value T) ResultOf[T] { return ResultOf[T]{value: value} }

// FailOf, verilen hatayla başarısız sonuç oluşturur; err nil olamaz.
func FailOf[T any](err *Error) ResultOf[T] {
	if err == nil {
		panic("sharedkernel: başarısız ResultOf hatasız oluşturulamaz")
	}
	return ResultOf[T]{err: err}
}

// IsSuccess, işlemin başarılı olup olmadığını döner.
func (r ResultOf[T]) IsSuccess() bool { return r.err == nil }

// IsFailure, işlemin başarısız olup olmadığını döner.
func (r ResultOf[T]) IsFailure() bool { return r.err != nil }

// Value, başarılı sonuçtaki değeri döner. Başarısız sonuçta çağırmak
// programlama hatasıdır ve panic üretir (.NET'teki InvalidOperationException karşılığı).
func (r ResultOf[T]) Value() T {
	if r.err != nil {
		panic("sharedkernel: başarısız ResultOf üzerinde Value çağrıldı: " + r.err.Error())
	}
	return r.value
}

// Err, başarısız sonuçtaki hatayı döner; başarılı sonuçta nil'dir.
func (r ResultOf[T]) Err() *Error { return r.err }
