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

    public async Task<List<Fact>> SeedAsync(BlossomSpace space, Post question)
    {
        var seed = await translator.AskAsync(new SpaceDiscoveryQuestion(question));

        var facts = seed.Value!.Facts.Select(x => new Fact(space, x)).ToList();
        var questions = seed.Value!.Questions.Select(x => new Question(space, x)).ToList();
        space.Vector.Text = seed.Value!.InitialAnswer;
        
        await vectorizer.VectorizeAsync([.. facts, .. questions, space]);

        var guideVectors = facts.Select(g => g.Vector).ToList();
        guideVectors.ForEach(x => x.CalculateLocalCoherence(guideVectors.Except([x]).ToList()));
        await posts.UpdateAsync(facts);
        await posts.UpdateAsync(questions);

        space.SetSummary(new(friendlyId.Create(), question.Text ?? "", ""));

        return facts;
    }

    internal async Task<Post> CalculateHintAsync(BlossomSpace currentLocation, Post lastPost, BlossomSpace destination)
    {
        var journey = destination.Vector.Subtract(currentLocation.Vector);

        var clues = await posts.SearchAsync(journey, 5);
        var question = new AnswerHintQuestion(destination, lastPost, clues);
        var hint = await translator.AskAsync(question);
        var hintPost = new Post(destination, BlossomUser.System.Avatar, hint.Value!.Text);

        await vectorizer.VectorizeAsync(hintPost);
        await posts.UpdateAsync([hintPost]);

        return hintPost;
    }
}
