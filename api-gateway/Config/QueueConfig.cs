namespace api_gateway.Config;

public static class QueueConfig
{
    public const string ExchangeName = "gateway_exchange";

    public static readonly Dictionary<string, List<string>> Queues = new()
    {
        {
            "user_service", new List<string>
            {
                "user.register",
                "user.login",
                "user.get_all",
                "user.get_by_id",
                "user.update",
                "user.delete"
            }
        },
        {
            "user_responses", new List<string>
            {
                "response.user.register",
                "response.user.login",
                "response.user.get_all",
                "response.user.get_by_id",
                "response.user.update",
                "response.user.delete"
            }
        },
        {
            "post_service", new List<string>
            {
                "post.create",
                "post.get_all",
                "post.get_by_id",
                "post.update",
                "post.delete",
                "post.like",
                "post.comment",
                "post.likes_get",
                "post.comments_get"
            }
        },
        {
            "post_responses", new List<string>
            {
                "response.post.create",
                "response.post.get_all",
                "response.post.get_by_id",
                "response.post.update",
                "response.post.delete",
                "response.post.like",
                "response.post.comment",
                "response.post.likes_get",
                "response.post.comments_get"
            }
        },
        {
            "image_service", new List<string>
            {
                "image.process"
            }
        },
        {
            "image_responses", new List<string>
            {
                "response.image.process"
            }
        }
    };

}