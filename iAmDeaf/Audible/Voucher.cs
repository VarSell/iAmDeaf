namespace iAmDeaf.Audible
{
    internal class Voucher
    {
        public class Rootobject
        {
            public Content_License content_license { get; set; }
            public string[] response_groups { get; set; }
        }

        public class Content_License
        {
            public string acr { get; set; }
            public string asin { get; set; }
            public Content_Metadata content_metadata { get; set; }
            public string drm_type { get; set; }
            public string license_id { get; set; }
            public License_Response license_response { get; set; }
            public string message { get; set; }
            public string request_id { get; set; }
            public bool requires_ad_supported_playback { get; set; }
            public string status_code { get; set; }
            public string voucher_id { get; set; }
        }

        public class Content_Metadata
        {
            public Chapter_Info chapter_info { get; set; }
            public Content_Reference content_reference { get; set; }
            public Content_Url content_url { get; set; }
            public Last_Position_Heard last_position_heard { get; set; }
        }

        public class Chapter_Info
        {
            public int brandIntroDurationMs { get; set; }
            public int brandOutroDurationMs { get; set; }
            public Chapter[] chapters { get; set; }
            public bool is_accurate { get; set; }
            public int runtime_length_ms { get; set; }
            public int runtime_length_sec { get; set; }
        }

        public class Chapter
        {
            public int length_ms { get; set; }
            public int start_offset_ms { get; set; }
            public int start_offset_sec { get; set; }
            public string title { get; set; }
        }

        public class Content_Reference
        {
            public string acr { get; set; }
            public string asin { get; set; }
            public string content_format { get; set; }
            public int content_size_in_bytes { get; set; }
            public string file_version { get; set; }
            public string marketplace { get; set; }
            public string sku { get; set; }
            public string tempo { get; set; }
            public string version { get; set; }
        }

        public class Content_Url
        {
            public string offline_url { get; set; }
        }

        public class Last_Position_Heard
        {
            public string status { get; set; }
        }

        public class License_Response
        {
            public string key { get; set; }
            public string iv { get; set; }
            public Rule[] rules { get; set; }
        }

        public class Rule
        {
            public Parameter[] parameters { get; set; }
            public string name { get; set; }
        }

        public class Parameter
        {
            public DateTime expireDate { get; set; }
            public string type { get; set; }
        }
    }
}
