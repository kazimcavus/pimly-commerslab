package application

import (
	"fmt"
	"strings"

	"pimly.commerslab/backend-go/internal/sharedkernel"
)

// Bu dosya, .NET tarafındaki FluentValidation kurallarının birebir portudur.
// Hata kodları ve mesajlar kablo formatının parçasıdır: açıkça kod atanmış
// kurallar sharedkernel kodlarını ("required"), atanmamışlar FluentValidation'ın
// varsayılan validator adlarını ("EmailValidator", "MinimumLengthValidator",
// "MaximumLengthValidator", "NotEmptyValidator") üretir. Alan adları kabloda
// snake_case döner (.NET'te DictionaryKeyPolicy bunu otomatik yapıyordu).

// isValidEmail, FluentValidation'ın ASP.NET uyumlu e-posta denetimini uygular:
// tam olarak bir '@', ilk veya son karakter olmamak koşuluyla.
func isValidEmail(value string) bool {
	at := strings.IndexByte(value, '@')
	return at > 0 && at == strings.LastIndexByte(value, '@') && at != len(value)-1
}

// validateEmailRules, e-posta alanının ortak kurallarını uygular
// (.NET IdentityValidationRules.Email portu; kurallar kısa devre yapmaz).
func validateEmailRules(email string, errs *[]sharedkernel.ValidationError) {
	if email == "" {
		*errs = append(*errs, sharedkernel.ValidationError{
			Field: "email", Code: sharedkernel.ValidationCodeRequired, Message: "Email is required."})
	}
	if !isValidEmail(email) {
		*errs = append(*errs, sharedkernel.ValidationError{
			Field: "email", Code: "EmailValidator", Message: "Email is not valid."})
	}
}

// validationFailure, biriken alan hatalarını .NET ile aynı özet mesajlı
// doğrulama hatasına çevirir; hata yoksa nil döner.
func validationFailure(errs []sharedkernel.ValidationError) *sharedkernel.Error {
	if len(errs) == 0 {
		return nil
	}
	return sharedkernel.NewValidationError("One or more validation errors occurred.", errs...)
}

// ValidateLoginCommand, giriş komutunun kurallarını uygular
// (.NET LoginCommandValidator portu).
func ValidateLoginCommand(cmd LoginCommand) *sharedkernel.Error {
	var errs []sharedkernel.ValidationError
	validateEmailRules(cmd.Email, &errs)
	if cmd.Password == "" {
		errs = append(errs, sharedkernel.ValidationError{
			Field: "password", Code: sharedkernel.ValidationCodeRequired, Message: "Password is required."})
	}
	return validationFailure(errs)
}

// ValidateRegisterUserCommand, kayıt komutunun kurallarını uygular
// (.NET RegisterUserCommandValidator portu).
func ValidateRegisterUserCommand(cmd RegisterUserCommand) *sharedkernel.Error {
	var errs []sharedkernel.ValidationError
	validateEmailRules(cmd.Email, &errs)

	if cmd.Password == "" {
		errs = append(errs, sharedkernel.ValidationError{
			Field: "password", Code: sharedkernel.ValidationCodeRequired, Message: "Password is required."})
	}
	if len([]rune(cmd.Password)) < 8 {
		errs = append(errs, sharedkernel.ValidationError{
			Field: "password", Code: "MinimumLengthValidator", Message: "Password must be at least 8 characters."})
	}

	if strings.TrimSpace(cmd.Name) != "" && len([]rune(cmd.Name)) > 200 {
		errs = append(errs, sharedkernel.ValidationError{
			Field: "name", Code: "MaximumLengthValidator",
			Message: fmt.Sprintf("The length of 'Name' must be 200 characters or fewer. You entered %d characters.", len([]rune(cmd.Name)))})
	}

	if cmd.TenantName != nil {
		if *cmd.TenantName == "" {
			errs = append(errs, sharedkernel.ValidationError{
				Field: "tenant_name", Code: "NotEmptyValidator", Message: "'Tenant Name' must not be empty."})
		}
		if len([]rune(*cmd.TenantName)) > 200 {
			errs = append(errs, sharedkernel.ValidationError{
				Field: "tenant_name", Code: "MaximumLengthValidator",
				Message: fmt.Sprintf("The length of 'Tenant Name' must be 200 characters or fewer. You entered %d characters.", len([]rune(*cmd.TenantName)))})
		}
	}
	return validationFailure(errs)
}
