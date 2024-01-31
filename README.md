# Internet Engineering Project - Backend

This repository alongside our [frontend](https://github.com/Alireza-Allahverdi/file-share) repository makes up the "File Management Server" project.

This server uses [minio](https://bitnami.com/stack/minio) as a file storage and stores the files while they are end-to-end encrypted.

Also, this project uses a [MongoDB)(https://hub.docker.com/_/mongo) database to store and manage the file system hierarchy and users.


## Access key permission

```json
{
	"Version": "2012-10-17",
	"Statement": [
		{
			"Effect": "Allow",
			"Action": ["s3:*"],
			"Resource": ["arn:aws:s3:::*"]
		}
	]
}
```

## Public access in buckets

```json
{
	"Version": "2012-10-17",
	"Statement": [
		{
			"Effect": "Allow",
			"Principal": {
				"AWS": ["*"]
			},
			"Action": [
				"s3:ListBucketMultipartUploads",
				"s3:GetBucketLocation",
				"s3:ListBucket"
			],
			"Resource": ["arn:aws:s3:::main"]
		},
		{
			"Effect": "Allow",
			"Principal": {
				"AWS": ["*"]
			},
			"Action": [
				"s3:AbortMultipartUpload",
				"s3:DeleteObject",
				"s3:GetObject",
				"s3:ListMultipartUploadParts",
				"s3:PutObject"
			],
			"Resource": ["arn:aws:s3:::main/*"]
		}
	]
}
```
