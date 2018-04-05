﻿namespace Xer.Cqrs.QueryStack
{
    public interface IQueryHandler<in TQuery, out TResult> where TQuery : IQuery<TResult>
    {
        /// <summary>
        /// Handle and process the query.
        /// </summary>
        /// <param name="query">Query to handle and process.</param>
        /// <returns>Result of the query.</returns>
        TResult Handle(TQuery query);
    }
}
