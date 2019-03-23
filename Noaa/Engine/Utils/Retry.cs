using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Engine
{
    public static class Retry
    {
        public static async Task RetryWebCallAsync(
            Func<Task> webCall,
            string name,
            int numberOfAttempts,
            int waitMilliseconds,
            bool rethrowWebException,
            Func<WebException, bool> onWebException = null)
        {
            await RetryWebCallAsync(
                async () => { await webCall(); return true; },
                name,
                numberOfAttempts,
                waitMilliseconds,
                rethrowWebException,
                onWebException);
        }

        public static async Task<TResult> RetryWebCallAsync<TResult>(
            Func<Task<TResult>> webCall,
            string name,
            int numberOfAttempts,
            int waitMilliseconds,
            bool rethrowWebException,
            Func<WebException, bool> onWebException = null)
        {
            while (true)
            {
                try
                {
                    return await webCall();
                }
                catch (Exception e)
                {
                    Logger.TraceException($"RetryWebCall({name})", e);

                    WebException webException = UnwrapAggregateException<WebException>(e);
                    if (webException == null)
                    {
                        throw;
                    }

                    if (onWebException != null)
                    {
                        if (!onWebException(webException))
                        {
                            numberOfAttempts = 0;
                        }
                    }

                    if (numberOfAttempts > 0)
                    {
                        --numberOfAttempts;
                    }

                    string errorResponse;
                    try
                    {
                        using (var streamReader = new StreamReader(webException.Response.GetResponseStream()))
                        {
                            errorResponse = await streamReader.ReadToEndAsync();
                        }
                    }
                    catch
                    {
                        errorResponse = null;
                    }

                    if (numberOfAttempts == 0)
                    {
                        if (rethrowWebException)
                        {
                            throw;
                        }
                        else
                        {
                            Logger.TraceLine($"RetryWebCall({name}): Web call failed and will NOT be retried. Last error response: {errorResponse}. Last exception: {e}");
                            return default(TResult);
                        }
                    }
                    else
                    {
                        Logger.TraceLine($"RetryWebCall({name}): Web call failed and will be retried in {waitMilliseconds}ms. Error response: {errorResponse}. Exception: {e}");
                        await Task.Delay(waitMilliseconds);
                    }
                }
            }
        }

        public static TResult UnwrapAggregateException<TResult>(Exception exception) where TResult: Exception
        {
            AggregateException aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                return UnwrapAggregateException<TResult>(aggregateException.InnerException);
            }
            else
            {
                return exception as TResult;
            }
        }
    }
}