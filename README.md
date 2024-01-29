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
