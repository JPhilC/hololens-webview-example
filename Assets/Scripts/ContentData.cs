using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class ContentTopic
{
    public string Title { get; set; }

    public string LandingPage { get; set; }
}
public class ContentFile
{
    public string Filename { get; set; }
    public DateTimeOffset DateModified { get; set; }

    [JsonIgnore]
    public bool Exists { get; set; }

    public List<ContentTopic> Topics { get; set; } = new List<ContentTopic>();
}

public class ContentData
{
    public List<ContentFile> Files { get; set; } = new List<ContentFile>();
}

public class Scenario
{
    public string Title { get; set; }

    public string ContentFile { get; set; }

    public string LandingPage { get; set; }
}
