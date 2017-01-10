using System.Collections.Generic;
using System.Net;
using ImageSharp;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.IO;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaImageConverter
{
    public class Functions
    {
        private AmazonS3Client s3Client;

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
            s3Client = new AmazonS3Client();
        }


        /// <summary>
        /// A Lambda function to respond to HTTP Get methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The list of blogs</returns>
        public async Task<APIGatewayProxyResponse> Get(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Get Request\n");
            Image testImage = await LoadImage(context);
            context.Logger.LogLine(testImage.ToString());

            var resizedImage = testImage.Resize(256, 256);
            resizedImage.SaveAsJpeg(new FileStream("/tmp/lena_resized.jpg", FileMode.Create));

            var putReq = new PutObjectRequest
            {
                BucketName = "lambda-image-converter",
                Key = "lena_resized.jpg",
                FilePath = "/tmp/lena_resized.jpg"
            };
            
            await s3Client.PutObjectAsync(putReq);

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "Hello AWS Serverless",
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };

            return response;
        }

        private async Task<Image> LoadImage(ILambdaContext context)
        {
            context.Logger.LogLine("Downloading image from S3...");
            var obj = await s3Client.GetObjectAsync("lambda-image-converter", "lena.jpg");
            context.Logger.LogLine("Image Download completed.");

            context.Logger.LogLine("Image reading...");
            Image img = new Image(obj.ResponseStream);
            context.Logger.LogLine("Image loaded.");

            return img;
        }
    }
}
