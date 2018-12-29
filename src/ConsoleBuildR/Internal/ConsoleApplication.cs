﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ConsoleBuildR.Internal
{
    internal class ConsoleApplication : IConsole
    {
        private readonly IServiceCollection _services;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;

        private IServiceProvider _applicationServices;
        private ExceptionDispatchInfo _applicationServicesException;

        public ConsoleApplication(
            IServiceCollection services,
            IServiceProvider serviceProvider,
            IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IServiceProvider Services
        {
            get
            {
                return _applicationServices;
            }
        }

        // Called immediately after the constructor so the properties can rely on it.
        public void Initialize()
        {
            try
            {
                EnsureApplicationServices();
            }
            catch (Exception ex)
            {
                // EnsureApplicationServices may have failed due to a missing or throwing Startup class.
                if (_applicationServices == null)
                {
                    _applicationServices = _services.BuildServiceProvider();
                }

                _applicationServicesException = ExceptionDispatchInfo.Capture(ex);
            }
        }

        private void EnsureApplicationServices()
        {
            if (_applicationServices == null)
            {
                throw new InvalidOperationException("No application services were configured");
            }
        }

        public void Dispose()
        {
            (_applicationServices as IDisposable)?.Dispose();
            (_serviceProvider as IDisposable)?.Dispose();
        }

        public void Run(string[] args)
        {
            var executables = _applicationServices.GetServices<IExecutable>();

            if (executables == null || !executables.Any()) throw new ArgumentNullException("No services were registered as a IExecutable. Call Run<ExecutableClass> in the ConsoleApplicationBuilder");

            var tasks = new List<Task>();

            foreach (var executable in executables)
            {
                tasks.Add(executable.Execute(args));
            }

            Task.WhenAll(tasks).GetAwaiter().GetResult();
        }

        public async Task RunAsync(string[] args)
        {
            var executables = _applicationServices.GetServices<IExecutable>();

            if (executables == null || !executables.Any()) throw new ArgumentNullException("No services were registered as a IExecutable. Call Run<ExecutableClass> in the ConsoleApplicationBuilder");

            var tasks = new List<Task>();

            foreach (var executable in executables)
            {
                tasks.Add(Task.Run(() => executable.Execute(args)));
            }

            await Task.WhenAll(tasks);
        }
    }
}
