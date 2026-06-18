// Package storage wraps the MinIO/S3 client used for media objects.
package storage

import (
	"context"
	"fmt"
	"io"
	"strings"

	"github.com/minio/minio-go/v7"
	"github.com/minio/minio-go/v7/pkg/credentials"
)

// Client is a thin wrapper over the MinIO SDK scoped to one bucket.
type Client struct {
	mc         *minio.Client
	bucket     string
	publicBase string
}

// Config configures the storage client.
type Config struct {
	Endpoint      string
	AccessKey     string
	SecretKey     string
	Bucket        string
	UseSSL        bool
	PublicBaseURL string
}

// New builds a storage client.
func New(cfg Config) (*Client, error) {
	mc, err := minio.New(cfg.Endpoint, &minio.Options{
		Creds:  credentials.NewStaticV4(cfg.AccessKey, cfg.SecretKey, ""),
		Secure: cfg.UseSSL,
	})
	if err != nil {
		return nil, fmt.Errorf("minio client: %w", err)
	}
	return &Client{
		mc:         mc,
		bucket:     cfg.Bucket,
		publicBase: strings.TrimRight(cfg.PublicBaseURL, "/"),
	}, nil
}

// EnsureBucket creates the bucket if it does not already exist and applies a
// public read-only policy so object URLs are fetchable directly by the browser.
func (c *Client) EnsureBucket(ctx context.Context) error {
	exists, err := c.mc.BucketExists(ctx, c.bucket)
	if err != nil {
		return fmt.Errorf("bucket exists: %w", err)
	}
	if !exists {
		if err := c.mc.MakeBucket(ctx, c.bucket, minio.MakeBucketOptions{}); err != nil {
			return fmt.Errorf("make bucket: %w", err)
		}
	}
	// Anonymous read access for objects (media URLs are public, unguessable keys).
	policy := fmt.Sprintf(`{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Principal": {"AWS": ["*"]},
    "Action": ["s3:GetObject"],
    "Resource": ["arn:aws:s3:::%s/*"]
  }]
}`, c.bucket)
	if err := c.mc.SetBucketPolicy(ctx, c.bucket, policy); err != nil {
		return fmt.Errorf("set bucket policy: %w", err)
	}
	return nil
}

// Upload stores an object and returns its public URL.
func (c *Client) Upload(ctx context.Context, objectName string, r io.Reader, size int64, contentType string) (string, error) {
	if contentType == "" {
		contentType = "application/octet-stream"
	}
	_, err := c.mc.PutObject(ctx, c.bucket, objectName, r, size, minio.PutObjectOptions{ContentType: contentType})
	if err != nil {
		return "", fmt.Errorf("put object: %w", err)
	}
	return c.publicBase + "/" + objectName, nil
}

// Delete removes the object whose public URL is given (best effort).
func (c *Client) DeleteByURL(ctx context.Context, url string) error {
	objectName := strings.TrimPrefix(url, c.publicBase+"/")
	if objectName == url {
		return nil // not one of ours
	}
	return c.mc.RemoveObject(ctx, c.bucket, objectName, minio.RemoveObjectOptions{})
}
