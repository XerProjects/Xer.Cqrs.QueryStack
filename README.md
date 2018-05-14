# Build

| Branch | Status |
|--------|--------|
| Master | [![Build status](https://ci.appveyor.com/api/projects/status/jr4h0o8h064m6je2/branch/master?svg=true)](https://ci.appveyor.com/project/XerProjects25246/xer-cqrs-5e3ne/branch/master) |
| Dev | [![Build status](https://ci.appveyor.com/api/projects/status/jr4h0o8h064m6je2/branch/dev?svg=true)](https://ci.appveyor.com/project/XerProjects25246/xer-cqrs-5e3ne/branch/dev) |


# Table of contents
* [Overview](#overview)
* [Features](#features)
* [Installation](#installation)
* [Getting Started](#getting-started)
   * [Sample Query And Query Handlers](#sample-query-and-query-handlers)
   * [Query Handler Registration](#query-handler-registration)
   * [Query Dispatcher Usage](#query-dispatcher-usage)

# Overview
Simple CQRS library

This project composes of components for implementing the CQRS pattern (Query Handling). This library was built with simplicity, modularity and pluggability in mind.

## Features
* Send queries to registered query handler.
* Multiple ways of registering handlers:
    * Simple handler registration (no IoC container).
    * IoC container registration - achieved by creating implementations of IContainerAdapter.
    * Attribute registration - achieved by marking methods with [QueryHandler] attributes.

## Installation
You can simply clone this repository, build the source, reference the dll from the project, and code away!

Xer.Cqrs libraries are also available as Nuget packages:
[![NuGet](https://img.shields.io/nuget/v/Xer.Cqrs.QueryStack.svg)](https://www.nuget.org/packages/Xer.Cqrs.QueryStack/)

To install Nuget packages:
1. Open command prompt
2. Go to project directory
3. Add the packages to the project:
    ```csharp
    dotnet add package Xer.Cqrs.QueryStack
    ```
4. Restore the packages:
    ```csharp
    dotnet restore
    ```

## Getting Started
(Samples are in ASP.NET Core)

### Sample Query And Query Handlers

```csharp
// Example query.
public class QueryProductById : IQuery<Product>
{
    public int ProductId { get; }

    public QueryProductById(int productId) 
    {
        ProductId = productId;
    }
}

// Async query handler.
public class QueryProductByIdHandler : IQueryAsyncHandler<QueryProductById, Product>
{
    private readonly IProductReadSideRepository _productRepository;
    
    public QueryProductByIdHandler(IProductReadSideRepository productRepository)
    {
        _productRepository = productRepository;    
    }

    public Task<Product> HandleAsync(QueryProductById query, CancellationToken cancellationToken = default(CancellationToken))
    {
        return _productRepository.GetProductByIdAsync(query.ProductId);
    }
}

// Sync query handler.
public class SyncQueryProductByIdHandler : IQueryHandler<QueryProductById, Product>
{
    private readonly IProductReadSideRepository _productRepository;
    
    public QueryProductByIdHandler(IProductReadSideRepository productRepository)
    {
        _productRepository = productRepository;    
    }

    public Product Handle(QueryProductById query)
    {
        return _productRepository.GetProductById(query.ProductId);
    }
}

// Attributed query handler.
public class QueryProductByIdHandler
{
    private readonly IProductReadSideRepository _productRepository;
    
    public QueryProductByIdHandler(IProductReadSideRepository productRepository)
    {
        _productRepository = productRepository;    
    }
    
    [QueryHandler]
    public Product Handle(QueryProductById query)
    {
        return _productRepository.GetProductById(query.ProductId);
    }
}
```

### Query Handler Registration

Before we can dispatch any queries, first, we need to register our query handlers. There are several ways to do this:

#### 1. Simple Registration (No IoC container)
```csharp
// This method gets called by the runtime. Use this method to add services to the container.
public void ConfigureServices(IServiceCollection services)
{            
    ...
    // Read-side repository.
    services.AddSingleton<IProductReadSideRepository, InMemoryProductReadSideRepository>();

    // Register query dispatcher.
    services.AddSingleton<IQueryAsyncDispatcher>((serviceProvider) =>
    {
        // This object implements IQueryHandlerResolver.
        var registration = new QueryHandlerRegistration();
        registration.Register(() => new QueryProductByIdHandler(serviceProvider.GetRequiredService<IProductReadSideRepository>()));

        return new QueryDispatcher(registration);
    });
    ...
}
```

#### 2. Container Registration
```csharp
// This method gets called by the runtime. Use this method to add services to the container.
public void ConfigureServices(IServiceCollection services)
{            
    ...
    // Read-side repository.
    services.AddSingleton<IProductReadSideRepository, InMemoryProductReadSideRepository>();
    
    // Register query handlers to the container.
    // Tip: You can use assembly scanners to scan for handlers.
    services.AddTransient<IQueryHandler<QueryProductById, Product>, SyncQueryProductByIdHandler>();

    // Register query dispatcher.
    services.AddSingleton<IQueryAsyncDispatcher>((serviceProvider) =>
        // The ContainerQueryHandlerResolver only resolves sync handlers. 
        // For async handlers, ContainerQueryAsyncHandlerResolver should be used.
        new QueryDispatcher(new ContainerQueryHandlerResolver(new AspNetCoreServiceProviderAdapter(serviceProvider)))
    );
    ...
}

// Container adapter.
class AspNetCoreServiceProviderAdapter : Xer.Cqrs.QueryStack.Resolvers.IContainerAdapter
{
    private readonly IServiceProvider _serviceProvider;

    public AspNetCoreServiceProviderAdapter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T Resolve<T>() where T : class
    {
        return _serviceProvider.GetService<T>();
    }
}
```

#### 3. Attribute Registration
```csharp
// This method gets called by the runtime. Use this method to add services to the container.
public void ConfigureServices(IServiceCollection services)
{            
    ...
    // Read-side repository.
    services.AddSingleton<IProductReadSideRepository, InMemoryProductReadSideRepository>();

    // Register query handler resolver. This is resolved by QueryDispatcher.
    services.AddSingleton<IQueryAsyncDispatcher>((serviceProvider) =>
    {
        // This implements IQueryHandlerResolver.
        var attributeRegistration = new QueryHandlerAttributeRegistration();
        // Register all methods with [QueryHandler] attribute.
        attributeRegistration.Register(() => new QueryProductByIdHandler(serviceProvider.GetRequiredService<IProductReadSideRepository>()));

        return new QueryDispatcher(attributeRegistration);
    });
    ...
}
```
### Query Dispatcher Usage
After setting up the query dispatcher in the IoC container, queries can now be dispatched by simply doing:
```csharp
...
private readonly IQueryAsyncDispatcher _queryDispatcher;

public ProductsController(IQueryAsyncDispatcher queryDispatcher)
{
    _queryDispatcher = queryDispatcher;
}

[HttpGet("{productId}")]
public async Task<IActionResult> GetProduct(int productId)
{
    Product product = await _queryDispatcher.DispatchAsync<QueryProductById, Product>(new QueryProductById(productId));
    if(product != null)
    {
        return Ok(product);
    }

    return NotFound();
}
...
```
