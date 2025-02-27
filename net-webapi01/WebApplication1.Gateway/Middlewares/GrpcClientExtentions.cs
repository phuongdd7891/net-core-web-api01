using Gateway.Interceptors;
using Userservice;
using Adminauthservice;
using Adminuserservice;
using Fileuploadservice;
using Booklibrary;

namespace Gateway.Middlewares;

public static class GrpcClientExtentions
{
    public static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGrpcClient<UserServiceProto.UserServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:UserMicroserviceUrl")!);
        }).AddInterceptor<ErrorHandlerInterceptor>();

        services.AddGrpcClient<AdminUserServiceProto.AdminUserServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:AdminMicroserviceUrl")!);
        }).AddInterceptor<ErrorHandlerInterceptor>();

        services.AddGrpcClient<AdminAuthServiceProto.AdminAuthServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:AdminMicroserviceUrl")!);
        }).AddInterceptor<ErrorHandlerInterceptor>();

        services.AddGrpcClient<UploadServiceProto.UploadServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:ManagementUserMicroserviceUrl")!);
        }).AddInterceptor<ErrorHandlerInterceptor>();

        services.AddGrpcClient<BookLibraryServiceProto.BookLibraryServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:ManagementUserMicroserviceUrl")!);
        }).AddInterceptor<ErrorHandlerInterceptor>();

        return services;
    }
}