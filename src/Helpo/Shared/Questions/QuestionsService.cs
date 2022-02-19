using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Helpo.Services;
using Helpo.Shared.Auth;
using Helpo.Shared.Users;
using Raven.Client.Documents;
using Raven.Client.Documents.Queries;
using static Helpo.Extensions.RavenDbExtensions;

namespace Helpo.Shared.Questions;

public class QuestionsService
{
    private readonly CurrentUserService _currentUserService;
    private readonly IDocumentStore _documentStore;
    private readonly IdFactory _idFactory;

    public QuestionsService(CurrentUserService currentUserService, IDocumentStore documentStore, IdFactory idFactory)
    {
        Guard.IsNotNull(currentUserService, nameof(currentUserService));
        Guard.IsNotNull(documentStore, nameof(documentStore));
        Guard.IsNotNull(idFactory, nameof(idFactory));

        this._idFactory = idFactory;
        this._documentStore = documentStore;
        this._currentUserService = currentUserService;        
    }

    public async Task<Question> AskQuestion(string title, string content, List<string> tags, CancellationToken cancellationToken)
    {
        Guard.IsNotNullOrWhiteSpace(title, nameof(title));
        Guard.IsNotNullOrWhiteSpace(content, nameof(content));
        Guard.IsNotNull(tags, nameof(tags));

        using var session = this._documentStore.OpenAsyncSession();
    
        var currentUser = await this._currentUserService.GetRequiredCurrentUser();

        var question = new Question 
        {
            Id = this._idFactory.GenerateId(),
            Title = title,
            Content = content,
            Tags = tags,
            AskedAt = DateTimeOffset.Now,
            AskedByUserId = currentUser.HelpoUserId
        };

        await session.StoreAsync(question, cancellationToken);

        await session.SaveChangesAsync(cancellationToken);

        return question;
    }

    public async Task<Question> EditQuestion(string questionId, string? newTitle, string? newContent, List<string>? newTags, CancellationToken cancellationToken)
    {
        Guard.IsNotNullOrWhiteSpace(questionId, nameof(questionId));
        // newTitle can be anything
        // newContent can be anything
        // newTags can be anything

        using var session = this._documentStore.OpenAsyncSession();

        var question = await session.RequiredLoadAsync<Question>(questionId, cancellationToken);
        var currentUser = await this._currentUserService.GetRequiredCurrentUser();

        // TODO: Allow moderators or admins to edit questions from other people
        if (question.AskedByUserId != currentUser.HelpoUserId)
            throw new Exception("You can only edit your own questions.");

        if (newTitle is not null)
            question.Title = newTitle;

        if (newContent is not null)
            question.Content = newContent;

        if (newTags is not null)
            question.Tags = newTags;
            
        await session.SaveChangesAsync(cancellationToken);

        return question;
    }

    public async Task<(Question, Answer)> AddAnswer(string questionId, string content, CancellationToken cancellationToken)
    {
        Guard.IsNotNullOrWhiteSpace(questionId, nameof(questionId));
        Guard.IsNotNullOrWhiteSpace(content, nameof(content));
        
        using var session = this._documentStore.OpenAsyncSession();

        var question = await session.RequiredLoadAsync<Question>(questionId, cancellationToken);
        var currentUser = await this._currentUserService.GetRequiredCurrentUser();

        var answer = new Answer
        {
            Id = this._idFactory.GenerateId(),
            Content = content,
            AnsweredAt = DateTimeOffset.Now,
            AnswerByUserId = currentUser.HelpoUserId,
        };
        question.Answers.Add(answer);
        
        await session.SaveChangesAsync(cancellationToken);

        return (question, answer);
    }

    public async Task<(Question, Answer)> EditAnswer(string questionId, string answerId, string newContent, CancellationToken cancellationToken)
    {
        Guard.IsNotNullOrWhiteSpace(questionId, nameof(questionId));
        Guard.IsNotNullOrWhiteSpace(answerId, nameof(answerId));
        Guard.IsNotNullOrWhiteSpace(newContent, nameof(newContent));
        
        using var session = this._documentStore.OpenAsyncSession();

        var question = await session.RequiredLoadAsync<Question>(questionId, cancellationToken);

        var answer = question.Answers.Single(f => f.Id == answerId);
        var currentUser = await this._currentUserService.GetRequiredCurrentUser();

        // TODO: Allow moderators or admins to edit answers from other people
        if (answer.AnswerByUserId != currentUser.HelpoUserId)
            throw new Exception("You can only edit your own answers.");

        answer.Content = newContent;
        
        await session.SaveChangesAsync(cancellationToken);

        return (question, answer);
    }

    public async Task<(List<QuestionViewModel> questions, int totalCount)> GetQuestions(int page, int pageSize, CancellationToken cancellationToken)
    {
        Guard.IsGreaterThanOrEqualTo(page, 0, nameof(page));
        Guard.IsGreaterThan(pageSize, 0, nameof(pageSize));

        using var session = this._documentStore.OpenAsyncSession();

        var questions = await session
            .Query<Question>()
            .Statistics(out var stats)
            .Skip(page * pageSize)
            .Take(pageSize)
            .OrderByDescending(f => f.AskedAt) //Newest questions first
            .Select(f => new QuestionViewModel 
            {
                QuestionId = f.Id,
                HasAcceptedAnswer = f.AcceptedAnswerId != null,
                Title = f.Title,
                Content = f.Content,
                Tags = f.Tags,
                AskedByUser = RavenQuery.Load<User>(f.AskedByUserId).Name,
            })
            .ToListAsync(cancellationToken);

        return (questions, stats.TotalResults);
    }
}


public class QuestionViewModel 
{
    public string QuestionId { get; set; } = default!;
    public bool HasAcceptedAnswer { get; set; }
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public List<string> Tags { get; set; } = new();
    public string AskedByUser { get; set; } = default!;
}