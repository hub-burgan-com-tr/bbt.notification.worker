namespace bbt.notification.worker
{
    public static class HealtCheckHelper
    {
        public static void WriteHealthy()
        {
            File.WriteAllText("/tmp/liveness_healthy", "OK");
        }

        public static void WriteUnhealthy()
        {
            File.WriteAllText("/tmp/liveness_healthy", "ERROR");
        }
    }
}