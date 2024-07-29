using Gateway.Interceptors;
using Gateway.Protos;
using Grpc.Core;

namespace Gateway.Middlewares;

public static class GrpcClientExtentions
{
    public static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGrpcClient<UserServiceProto.UserServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:UserMicroserviceUrl")!);
        });
        services.AddGrpcClient<AdminUserServiceProto.AdminUserServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:AdminMicroserviceUrl")!);
        }).AddInterceptor<ErrorHandlerInterceptor>();
        services.AddGrpcClient<AdminAuthServiceProto.AdminAuthServiceProtoClient>(c =>
        {
            c.Address = new Uri(configuration.GetValue<string>("Microservices:AdminMicroserviceUrl")!);
        });

        return services;
    }
}