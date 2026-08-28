// Package users, Identity modülünün kullanıcı kök varlığını ve ASP.NET
// Identity uyumlu şifre özetleme mantığını içerir (.NET Identity.Domain.Users
// + Identity.Infrastructure.Auth.PasswordService karşılığı).
package users

import (
	"crypto/hmac"
	"crypto/pbkdf2"
	"crypto/rand"
	"crypto/sha1"  //nolint:gosec // yalnızca eski V3/SHA1 hash'lerini doğrulamak için
	"crypto/sha256"
	"crypto/sha512"
	"encoding/base64"
	"encoding/binary"
	"errors"
	"hash"
)

// ASP.NET Core Identity V3 şifre özet biçimi (PasswordHasher varsayılanları).
// Base64 çözülmüş blob düzeni:
//
//	[0]      biçim işareti 0x01 (V3)
//	[1..5)   PRF (uint32, big-endian): 0=HMAC-SHA1, 1=HMAC-SHA256, 2=HMAC-SHA512
//	[5..9)   PBKDF2 yineleme sayısı (uint32, big-endian)
//	[9..13)  tuz uzunluğu (uint32, big-endian)
//	[13..)   tuz + 32 baytlık türetilmiş anahtar (subkey)
//
// Mevcut kullanıcıların .NET tarafında üretilmiş hash'leriyle giriş
// yapabilmesi için bu biçim bayt uyumlu uygulanır; Go tarafı da .NET emekli
// olana kadar AYNI biçimde yazar ki geri dönüş (rollback) güvenli kalsın.
const (
	formatMarkerV3   = 0x01
	prfHMACSHA1      = 0
	prfHMACSHA256    = 1
	prfHMACSHA512    = 2
	hashIterations   = 100_000
	hashSaltLength   = 16
	hashSubkeyLength = 32
)

// errMalformedHash, çözülemeyen veya desteklenmeyen hash biçimini işaretler.
var errMalformedHash = errors.New("users: şifre özeti biçimi tanınmadı")

// HashPassword, düz metin şifreyi ASP.NET Identity V3 biçiminde özetler
// (PBKDF2-HMAC-SHA512, 100.000 yineleme, 16 bayt rastgele tuz, 32 bayt anahtar).
func HashPassword(password string) (string, error) {
	salt := make([]byte, hashSaltLength)
	if _, err := rand.Read(salt); err != nil {
		return "", err
	}
	subkey, err := pbkdf2.Key(sha512.New, password, salt, hashIterations, hashSubkeyLength)
	if err != nil {
		return "", err
	}

	blob := make([]byte, 13+hashSaltLength+hashSubkeyLength)
	blob[0] = formatMarkerV3
	binary.BigEndian.PutUint32(blob[1:5], prfHMACSHA512)
	binary.BigEndian.PutUint32(blob[5:9], hashIterations)
	binary.BigEndian.PutUint32(blob[9:13], hashSaltLength)
	copy(blob[13:], salt)
	copy(blob[13+hashSaltLength:], subkey)
	return base64.StdEncoding.EncodeToString(blob), nil
}

// VerifyPassword, düz metin şifreyi saklanan V3 özetine karşı sabit zamanlı
// olarak doğrular. PRF, yineleme sayısı ve tuz uzunluğu blob'dan okunur;
// böylece farklı parametrelerle üretilmiş eski hash'ler de doğrulanır.
// Bozuk/desteklenmeyen biçim güvenli tarafta kalır ve false döner.
func VerifyPassword(password, storedHash string) bool {
	blob, err := base64.StdEncoding.DecodeString(storedHash)
	if err != nil || len(blob) < 13 || blob[0] != formatMarkerV3 {
		return false
	}

	prf := binary.BigEndian.Uint32(blob[1:5])
	iterations := int(binary.BigEndian.Uint32(blob[5:9]))
	saltLen := int(binary.BigEndian.Uint32(blob[9:13]))
	if iterations < 1 || saltLen < 8 || len(blob) < 13+saltLen+1 {
		return false
	}

	var newHash func() hash.Hash
	switch prf {
	case prfHMACSHA1:
		newHash = sha1.New
	case prfHMACSHA256:
		newHash = sha256.New
	case prfHMACSHA512:
		newHash = sha512.New
	default:
		return false
	}

	salt := blob[13 : 13+saltLen]
	expected := blob[13+saltLen:]
	actual, err := pbkdf2.Key(newHash, password, salt, iterations, len(expected))
	if err != nil {
		return false
	}
	return hmac.Equal(expected, actual)
}

// ensureSupportedHash, testlerde biçim denetimi için içsel doğrulayıcıdır;
// dışa açılmaz (yalnızca hata sabitine erişimi anlamlandırır).
var _ = errMalformedHash
