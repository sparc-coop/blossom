using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;

namespace Sparc.Blossom.Spaces;

public class Post : BlossomSpaceObject
{
    public Post() : base(Guid.NewGuid().ToString())
    {
        User = BlossomUser.System.Avatar;
    }

    public Post(BlossomSpace space, BlossomAvatar user, string text)
        : this(space.Id, user, text)
    {
    }

    public Post(string spaceId, BlossomAvatar user, string text)
        : this()
    {
        SpaceId = spaceId;
        User = user;
        Text = text;
        Vector = new(text);
    }

    public string? Text { get; set; }
    public List<SparcEntity> Entities { get; set; } = [];
    public string? ConstellationId { get; set; }
    public string? ConstellationConnectorId { get; set; }

    public async Task ExtractEntities(ISparcContent tovik, List<SparcEntityType> entityTypes)
    {
        Entities = await tovik.ExtractGraphAsync(new(this, entityTypes));
    }

    public void SetConstellation(Constellation? constellation, Post? connectToPost)
    {
        ConstellationId = constellation?.Id;
        ConstellationConnectorId = connectToPost?.Id;
    }

    //public async Task ExtractGraph(ISparcContent tovik)
    //{
    //    List<SparcEntityType> entityTypes = [
    //        new("Person", "A single human individual identified by their name"),
    //        new("Group", "An organization or collection of individuals identified by their name"),
    //        new("Topic", "Subject or theme that the idea relates to"),
    //        new("Hypothesis", "Explicitly stated proposition under test"),
    //        new("Method", "Scientific method variant, algorithm, or protocol"),
    //        new("Metric", "Success measure or evaluation standard"),
    //        new("Experiment", "Structured attempt to test a hypothesis"),
    //        new("Dataset", "Data used in reasoning or testing"),
    //        new("Result", "Outcome of an experiment or analysis"),
    //        new("Decision", "Group agreement, vote, or conclusion"),
    //        new("Location", "Physical or digital setting"),
    //        new("Constraint", "Funding, tools, or other limitations")
    //    ];

    //    Entities = await tovik.ExtractGraphAsync(new(this, entityTypes));
    //}
}
