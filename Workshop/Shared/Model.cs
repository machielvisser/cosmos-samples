public class User
{
    public Guid id { get; set; }
    public string username { get; set; }
}

public class SharesContainer
{
    public string type { get; set; }

    public SharesContainer(string type)
    {
        this.type = type;
    }
}

public class Post : SharesContainer
{
    public Post() : base("post") { }

    public Guid id { get; set; }
    public Guid userId { get; set; }
    public Guid postId { get => id; }
    public string content { get; set; }
    public DateTime creationDate { get; set; }
}

public class Comment : SharesContainer
{
    public Comment() : base("comment") { }

    public Guid id { get; set; }
    public Guid userId { get; set; }
    public Guid postId { get; set; }
    public string content { get; set; }
    public DateTime creationDate { get; set; }
}

public class Like : SharesContainer
{
    public Like() : base("like") { }

    public Guid id { get; set; }
    public Guid userId { get; set; }
    public Guid postId { get; set; }
    public DateTime creationDate { get; set; }
}
