using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;

namespace Sparc.Blossom.Spaces;

internal class BlossomSpaceTranslator
    (IEnumerable<ITranslator> translators,
    BlossomPosts posts,
    VoyageTranslator vectorizer,
    FriendlyId friendlyId)
{
    readonly AITranslator translator = translators.OfType<AITranslator>().First();

    public async Task<List<Guide>> SeedAsync(BlossomSpace space, Post question)
    {
        var discovery = new SpaceDiscoveryQuestion(question);
        var statements = await translator.AskAsync(discovery);

        var guides = statements.Value!.Statements.Select(x => new Guide(space, x)).ToList();
        await vectorizer.VectorizeAsync(guides);
        var guideVectors = guides.Select(g => g.Vector).ToList();
        guideVectors.ForEach(x => x.CalculateLocalCoherence(guideVectors.Except([x]).ToList()));
        await posts.UpdateAsync(guides);

        space.Update(guides);

        space.SetSummary(new(friendlyId.Create(), question.Text ?? "", ""));

        return guides;
    }

    internal async Task<Post> CalculateHintAsync(BlossomSpace currentLocation, Post lastPost, BlossomSpace destination)
    {
        var journey = destination.Vector.Subtract(currentLocation.Vector);

        var clues = await posts.SearchAsync(destination.Id, journey, 5);
        var question = new AnswerHintQuestion(destination, lastPost, clues);
        var hint = await translator.AskAsync(question);
        var hintPost = new Post(destination, BlossomUser.System.Avatar, hint.Value!.Text);

        await vectorizer.VectorizeAsync(hintPost);
        await posts.UpdateAsync([hintPost]);

        return hintPost;
    }
}
