// Go cmd line: dev setup
// go mod init healthaware
// go get github.com/aws/aws-sdk-go/aws
// go get github.com/aws/aws-sdk-go/aws/session
// go get github.com/aws/aws-sdk-go/service/s3
// go build -o main main.go
// go run main.go

package main

import (
	"fmt"
	"reflect"
	"time"
	"github.com/aws/aws-sdk-go/aws"
	"github.com/aws/aws-sdk-go/aws/session"
	"github.com/aws/aws-sdk-go/service/s3"
)

// Function that mocks an AWS session
func newMockSession() *session.Session {
	sess, err := session.NewSession(&aws.Config{
		Region: aws.String("us-west-2"),
	})
	if err != nil {
		panic(err)
	}

	return sess
}

// Function that mocks an S3 client
func newMockS3Client(sess *session.Session) *s3.S3 {
	// Create a new S3 client
	svc := s3.New(sess)

	return svc
}

// Function that gets the AWS Account ID from a mock session
 func getAccountID(sess *session.Session) string {
 	// Get the AWS Account information from session
 	creds, err := sess.Config.Credentials.Get()
 	if err != nil {
 		panic(err)
 	}
 	return creds.AccountID
 }

// Function to check type of object with reflection
func checkType(obj interface{}) string {
	return fmt.Sprintf("%T", obj)
}

func getDate() (time.Time, error) {
	currentDate := time.Now()

	if reflect.TypeOf(currentDate) != reflect.TypeOf(time.Time{}) {
		return time.Time{}, fmt.Errorf("currentDate is not of type time.Time - check 3")
	}

	return currentDate, nil
}

// Function that returns a formatted date
func getFormattedDate() (string, error) {
	currentDate := time.Now()

	checkCurrentDate := checkType(currentDate)
	if checkCurrentDate != "time.Time" {
		return "", fmt.Errorf("currentDate is not of type time.Time - check 1")
	}

	if reflect.TypeOf(currentDate) != reflect.TypeOf(time.Time{}) {
		return "", fmt.Errorf("currentDate is not of type time.Time - check 2")
	}

	formattedDate := currentDate.Format("2006-01-02")
	return formattedDate, nil
}

func getFormattedTime() string {
	currentDate := time.Now()
	formattedTime := currentDate.Format("12:00")

	return formattedTime
}

// main is the primary entrypoint for the application
func main() {
	dateTime, err := getDate()
	if err != nil {
		panic(err)
	}
	println(dateTime.Format("2006-01-02 15:04:05"))

	formattedDate, err := getFormattedDate()
	if err != nil {
		panic(err)
	}
	println(formattedDate)

	sess := newMockSession()
	println(*sess.Config.Region)

	creds, err := sess.Config.Credentials.Get()
	if err != nil {
		panic(err)
	}
	println(creds.AccessKeyID)

	s3Client := newMockS3Client(sess)
	println(*s3Client.Config.Region)

	// Create Create Bucket Input parameters
	newS3BucketInput := &s3.CreateBucketInput{
		Bucket: aws.String("core-aha-bucket1"),
	}
	s3BucketCreateResult, s3BucketCreateErr := s3Client.CreateBucket(newS3BucketInput)
	if s3BucketCreateErr != nil {
		panic(s3BucketCreateErr)
	}
	println(s3BucketCreateResult)

	s3BucketListResult, s3BucketListErr := s3Client.ListBuckets(nil)
	if s3BucketListErr != nil {
		panic(s3BucketListErr)
	}
	println(s3BucketResult)

	for _, bucket := range s3BucketListResult.Buckets {
		println(*bucket.Name)
	}

	// Create Delete Bucket Input parameters
	deleteBucketInput := &s3.DeleteBucketInput{
		Bucket: aws.String("core-aha-bucket1"),
	}
	s3DeleteBucketResult, s3DeleteBucketErr := s3Client.DeleteBucket(deleteBucketInput)
	if s3DeleteBucketErr != nil {
		panic(s3DeleteBucketErr)
	}
	println(s3DeleteBucketResult)

}
