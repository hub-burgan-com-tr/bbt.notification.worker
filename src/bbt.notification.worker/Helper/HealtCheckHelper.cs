namespace bbt.notification.worker
{
    public static class HealtCheckHelper
    {
        private static string path = "/tmp/liveness_healthy1";

        public static void WriteHealthy()
        {
            if (File.Exists(path))
                File.WriteAllText(path, "OK"); 
        }

        public static void WriteUnhealthy()
        {
            if (File.Exists(path))
                File.WriteAllText(path, "ERROR");
        }
    }
}