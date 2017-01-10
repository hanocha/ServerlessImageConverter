using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using ImageSharp;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaImageConverter
{
    public class Functions
    {
        private AmazonS3Client s3Client;
        private Stopwatch globalSw;

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
            globalSw = Stopwatch.StartNew();
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

            var sw = Stopwatch.StartNew();
            Image testImage = await LoadImage(context);
            sw.Stop(); Logging("Load image time: " + sw.Elapsed.ToString(), context);

            sw.Restart();
            var resizedImage = testImage.Resize(256, 256);
            sw.Stop(); Logging("Resize image time: " + sw.Elapsed.ToString(), context);

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

            globalSw.Stop(); Logging("Total time: " + globalSw.Elapsed.ToString(), context);
            return response;
        }

        private async Task<Image> LoadImage(ILambdaContext context)
        {
            var obj = await s3Client.GetObjectAsync("lambda-image-converter", "lena.jpg");
            Image img = new Image(obj.ResponseStream);
            return img;
        }

        private void Logging(string str, ILambdaContext context)
        {
            context.Logger.LogLine(str);
        }
    }
}
