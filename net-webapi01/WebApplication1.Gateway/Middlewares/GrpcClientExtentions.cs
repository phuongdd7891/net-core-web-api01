using Gateway.Interceptors;
using Userservice;
using Adminauthservice;
using Adminuserservice;
using Fileuploadservice;
using Booklibrary;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Gateway.Middlewares;

public static class GrpcClientExtentions
{
    public static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration)
    {
        var httpHandler = new HttpClientHandler();
        httpHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            if (errors == SslPolicyErrors.None) return true;
            try
            {
                var expectedCert = new X509Certificate2("cert.pem");
                bool certsMatch = cert?.Thumbprint == expectedCert.Thumbprint;
                return certsMatch;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{cert?.Subject} - Validation Error: {ex.Message}");
                return false;
            }
        };

        services.AddGrpcClient<UserServiceProto.UserServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:UserMicroserviceUrl")!);
            c.ChannelOptionsActions.Add(o =>
            {
                o.HttpHandler = httpHandler;
            });
        }).AddInterceptor<ErrorHandlerInterceptor>();

        services.AddGrpcClient<AdminUserServiceProto.AdminUserServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:AdminMicroserviceUrl")!);
            c.ChannelOptionsActions.Add(o =>
            {
                o.HttpHandler = httpHandler;
            });
        }).AddInterceptor<ErrorHandlerInterceptor>();

        services.AddGrpcClient<AdminAuthServiceProto.AdminAuthServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:AdminMicroserviceUrl")!);
            c.ChannelOptionsActions.Add(o =>
            {
                o.HttpHandler = httpHandler;
            });
        }).AddInterceptor<ErrorHandlerInterceptor>();

        services.AddGrpcClient<UploadServiceProto.UploadServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:ManagementUserMicroserviceUrl")!);
            c.ChannelOptionsActions.Add(o =>
            {
                o.HttpHandler = httpHandler;
            });
        }).AddInterceptor<ErrorHandlerInterceptor>();

        services.AddGrpcClient<BookLibraryServiceProto.BookLibraryServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:ManagementUserMicroserviceUrl")!);
            c.ChannelOptionsActions.Add(o =>
            {
                o.HttpHandler = httpHandler;
            });
        }).AddInterceptor<ErrorHandlerInterceptor>();

        return services;
    }
}