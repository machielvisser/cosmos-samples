public class User
{
    public Guid id { get; set; }
    public string username { get; set; }
}

public class Post
{
    public Guid id { get; set; }
    public Guid userId { get; set; }
    public string content { get; set; }
    public DateTime creationDate { get; set; }
}
