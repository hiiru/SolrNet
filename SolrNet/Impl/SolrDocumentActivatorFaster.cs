using System;
using System.Linq.Expressions;

namespace SolrNet.Impl
{
    /// <summary>
    /// Creates a new instance of document type <typeparamref name="T"/> using <see cref="Expression"/>, faster than SolrDocumentActivator but <typeparamref name="T"/> requires a public parameterless constructor.
    /// In my case it took ~5-10 ms less for 65k Objects.
    /// </summary>
    /// <typeparam name="T">document type</typeparam>
    /// <seealso cref="http://www.smelser.net/blog/post/2010/03/05/When-Activator-is-just-to-slow.aspx"/>
    /// <seealso cref="http://stackoverflow.com/questions/367577/why-does-the-c-sharp-compiler-emit-activator-createinstance-when-calling-new-in"/>
    public class SolrDocumentActivatorFaster<T> : ISolrDocumentActivator<T>  where T : new()
    {
        private static readonly Expression<Func<T>> ConstructorExpression = () => new T();
        private static readonly Func<T> Constructor = ConstructorExpression.Compile();
        public T Create()
        {
            return Constructor();
        }
    }
}
