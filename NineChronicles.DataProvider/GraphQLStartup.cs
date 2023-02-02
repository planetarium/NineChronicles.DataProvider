namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Concurrent;
    using GraphQL.Server;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using NineChronicles.DataProvider.GraphTypes;
    using NineChronicles.Headless;
    using NineChronicles.Headless.GraphTypes;
    using Sentry;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    public class GraphQLStartup
    {
        public GraphQLStartup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();

            services.AddControllers();
            services.AddGraphQL(
                    (options, provider) =>
                    {
                        options.EnableMetrics = true;
                        options.UnhandledExceptionDelegate = context =>
                        {
                            Console.Error.WriteLine(context.Exception.ToString());
                            Console.Error.WriteLine(context.ErrorMessage);
                        };
                    })
                .AddSystemTextJson()
                .AddWebSockets()
                .AddDataLoader()
                .AddGraphTypes(typeof(NineChroniclesSummarySchema))
                .AddGraphTypes(typeof(StandaloneSchema))
                .AddLibplanetExplorer<NCAction>()
                .AddUserContextBuilder<UserContextBuilder>();
            services.AddGraphTypes();
            services.AddSingleton<NineChroniclesSummarySchema>();
            services.AddSingleton<StandaloneSchema>();
            services.AddSingleton<ConcurrentDictionary<string, ITransaction>>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("AllowAllOrigins");
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health-check");
            });

            app.UseGraphQL<NineChroniclesSummarySchema>();
            app.UseGraphQL<StandaloneSchema>("/graphql_headless");
            app.UseGraphQLPlayground();
        }
    }
}
