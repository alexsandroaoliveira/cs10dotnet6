using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Packt.Shared;

namespace Northwind.WebApi.Repositories;


public class CustomerRepository : ICustomerRepository
{
    // use a static thread-safe dictionary field to cache the customers
    private static ConcurrentDictionary
        <string, Customer>? customerCache;

    // use an instance data context field because it should not be
    // cached due to their internal caching
    private NorthwindContext db;

    public CustomerRepository(NorthwindContext injectedContext)
    {
        db = injectedContext;

        // pre-load customers from database as a normal
        // Dictionary with CustomerId as the key,
        // then convert to a thread-safe ConcurrentDictionary
        if (customerCache is null)
        {
            customerCache = new ConcurrentDictionary<string, Customer>(
                db.Customers.ToDictionary(c => c.CustomerId));
        }
    }

    public async Task<Customer?> CreateAsync(Customer c)
    {
        // normalize CustomerId into uppercase
        c.CustomerId = c.CustomerId.ToUpper();

        // add to database using EF Core
        EntityEntry<Customer> added = await db.Customers.AddAsync(c);
        int affected = await db.SaveChangesAsync();
        if (affected == 1)
        {
            if (customerCache is null) return c;
            // if the customer is new, add it to cache, else
            // call UpdateCache method
            return customerCache.AddOrUpdate(c.CustomerId, c, UpdateCache);
        }
        else
        {
            return null;
        }
    }

    public Task<IEnumerable<Customer>> RetrieveAllAsync()
    {
        // for performance, get from cach
        return Task.FromResult(customerCache is null
            ? Enumerable.Empty<Customer>() : customerCache.Values);
    }

    public Task<Customer?> RetrieveAsync(string id)
    {
        // for performance, get from cache
        id = id.ToUpper();
        if (customerCache is null) return null!;
        customerCache.TryGetValue(id, out Customer? c);

        return Task.FromResult(c);
    }

    private Customer UpdateCache(string id, Customer c)
    {
        Customer? old;
        if (customerCache is not null)
        {
            if (customerCache.TryGetValue(id, out old))
            {
                if (customerCache.TryUpdate(id, c, old))
                {
                    return c;
                }
            }
        }
        return null!;
    }

    public async Task<Customer?> UpdateAsync(string id, Customer c)
    {
        // normalize customer Id
        id = id.ToUpper();
        c.CustomerId = c.CustomerId.ToUpper();

        // update in database
        db.Customers.Update(c);
        int affected = await db.SaveChangesAsync();
        if (affected == 1)
        {
            /// update in cache
            return UpdateCache(id, c);
        }
        return null;
    }

    public async Task<bool?> DeleteAsync(string id)
    {
        // normalize customer Id
        id = id.ToUpper();

        // remove from database
        Customer? c = db.Customers.Find(id);
        if (c is null) return null;
        db.Customers.Remove(c);
        int affected = await db.SaveChangesAsync();

        if (affected == 1)
        {
            if (customerCache is null) return null;
            // remove from cache
            return customerCache.TryRemove(id, out c);
        }
        else
        {
            return null;
        }
    }
}