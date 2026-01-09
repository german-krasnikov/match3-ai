// Services.cs
namespace Core
{
    public static class Services
    {
        private static ServiceLocator _locator;

        public static void Initialize(ServiceLocator locator) => _locator = locator;
        public static T Get<T>() where T : class => _locator.Get<T>();
        public static bool TryGet<T>(out T service) where T : class => _locator.TryGet(out service);
        public static void Reset() => _locator = null;
    }
}
