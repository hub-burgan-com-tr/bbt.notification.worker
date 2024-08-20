namespace bbt.notification.worker
{
    public static class HealtCheckHelper
    {
        private static string path = "/tmp/liveness_healthy";

        public static void WriteHealthy()
        {
            File.WriteAllText(path, "OK");
        }

        public static void WriteUnhealthy()
        {
            File.WriteAllText(path, "ERROR");
        }
    }
}