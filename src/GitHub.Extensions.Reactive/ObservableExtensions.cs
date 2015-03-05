﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace GitHub.Extensions.Reactive
{
    public static class ObservableExtensions
    {
        public static IObservable<T> WhereNotNull<T>(this IObservable<T> observable) where T : class
        {
            return observable.Where(item => item != null);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "Rx has your back.")]
        public static IObservable<T> PublishAsync<T>(this IObservable<T> observable)
        {
            var ret = observable.Multicast(new AsyncSubject<T>());
            ret.Connect();
            return ret;
        }

        /// <summary>
        /// Used in cases where we only care when an observable completes and not what the observed values are.
        /// This is commonly used by some of our "Refresh" methods.
        /// </summary>
        public static IObservable<Unit> AsCompletion<T>(this IObservable<T> observable)
        {
            return observable
                .SelectMany(_ => Observable.Empty<Unit>())
                .Concat(Observable.Return(Unit.Default));
        }

        /// <summary>
        /// Subscribes to and produces values from the observable returned 
        /// by <paramref name="selector"/> once the source <paramref name="observable"/> 
        /// has completed while ignoring all elements produced from the source.
        /// </summary>
        /// <typeparam name="T">The type of the source observable</typeparam>
        /// <typeparam name="TRet">The type of the resulting observable.</typeparam>
        /// <param name="observable">The source observable.</param>
        /// <param name="selector">The selector for producing the return observable.</param>
        public static IObservable<TRet> ContinueAfter<T, TRet>(this IObservable<T> observable, Func<IObservable<TRet>> selector)
        {
            return observable.AsCompletion().SelectMany(_ => selector());
        }

        /// <summary>
        /// Helper method to transform an IObservable{T} to IObservable{Unit}.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="observable"></param>
        /// <returns></returns>
        public static IObservable<Unit> SelectUnit<T>(this IObservable<T> observable)
        {
            return observable.Select(_ => Unit.Default);
        }

        public static IObservable<TSource> CatchNonCritical<TSource>(
    this IObservable<TSource> first,
    Func<Exception, IObservable<TSource>> second)
        {
            return first.Catch<TSource, Exception>(e =>
            {
                if (!e.IsCriticalException())
                    return second(e);

                return Observable.Throw<TSource>(e);
            });
        }

        public static IObservable<TSource> CatchNonCritical<TSource>(
            this IObservable<TSource> first,
            IObservable<TSource> second)
        {
            return first.CatchNonCritical(e => second);
        }

    }
}
