using Exceptional.Core.Stores;

namespace Exceptional.Core
{
    public static class ErrorStore
    {
        private static SQLErrorStore _sqlErrorStore;

        public static void Setup(SQLErrorStore store)
        {
            _sqlErrorStore = store;
        }

        public static void LogException(Error error)
        {
            _sqlErrorStore.LogError(error);
        }
        
      
    }
}
