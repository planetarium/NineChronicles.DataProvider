﻿namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Concurrent;
    using GraphQL.Server;
    using GraphQL.Server.Transports.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using NineChronicles.DataProvider.GraphTypes;
    using NineChronicles.Headless;
    using NineChronicles.Headless.GraphTypes;
    using NineChronicles.Headless.Repositories.BlockChain;
    using NineChronicles.Headless.Repositories.StateTrie;
    using NineChronicles.Headless.Repositories.Transaction;
    using NineChronicles.Headless.Repositories.WorldState;

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
                .AddLibplanetExplorer();
            services.AddSingleton<StateMemoryCache>();
            services.AddGraphTypes();
            services.AddSingleton<IWorldStateRepository, WorldStateRepository>();
            services.AddSingleton<ITransactionRepository, TransactionRepository>();
            services.AddSingleton<IBlockChainRepository, BlockChainRepository>();
            services.AddSingleton<IStateTrieRepository, StateTrieRepository>();
            services.AddSingleton<NineChroniclesSummarySchema>();
            services.AddSingleton<StandaloneSchema>();
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

            app.UseGraphQL<NineChroniclesSummarySchema>("/graphql_dp");
            app.UseGraphQL<StandaloneSchema>();
            app.UseGraphQLPlayground();
        }
    }
}
