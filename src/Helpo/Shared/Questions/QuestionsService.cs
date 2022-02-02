using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Helpo.Services;
using Helpo.Shared.Auth;
using Raven.Client.Documents.Session;
using static Helpo.Extensions.RavenDbExtensions;

namespace Helpo.Shared.Questions;

public class QuestionsService
{
    private readonly CurrentUserService _currentUserService;
    private readonly IAsyncDocumentSession _documentSession;
    private readonly IdFactory _idFactory;

    public QuestionsService(CurrentUserService currentUserService, IAsyncDocumentSession documentSession, IdFactory idFactory)
    {
        Guard.IsNotNull(currentUserService, nameof(currentUserService));
        Guard.IsNotNull(documentSession, nameof(documentSession));
        Guard.IsNotNull(idFactory, nameof(idFactory));

        this._idFactory = idFactory;
        this._documentSession = documentSession;
        this._currentUserService = currentUserService;        
    }

    public async Task<Question> AskQuestion(string title, string content, List<string> tags)
    {
        Guard.IsNotNullOrWhiteSpace(title, nameof(title));
        Guard.IsNotNullOrWhiteSpace(content, nameof(content));
        Guard.IsNotNull(tags, nameof(tags));

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

        await this._documentSession.StoreAsync(question);

        return question;
    }

    public async Task<Question> EditQuestion(string questionId, string? newTitle, string? newContent, List<string>? newTags)
    {
        Guard.IsNotNullOrWhiteSpace(questionId, nameof(questionId));
        // newTitle can be anything
        // newContent can be anything
        // newTags can be anything

        var question = await this._documentSession.RequiredLoadAsync<Question>(questionId);
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

        return question;
    }

    public async Task<(Question, Answer)> AddAnswer(string questionId, string content)
    {
        Guard.IsNotNullOrWhiteSpace(questionId, nameof(questionId));
        Guard.IsNotNullOrWhiteSpace(content, nameof(content));
        
        var question = await this._documentSession.RequiredLoadAsync<Question>(questionId);
        var currentUser = await this._currentUserService.GetRequiredCurrentUser();

        var answer = new Answer
        {
            Id = this._idFactory.GenerateId(),
            Content = content,
            AnsweredAt = DateTimeOffset.Now,
            AnswerByUserId = currentUser.HelpoUserId,
        };
        question.Answers.Add(answer);

        return (question, answer);
    }

    public async Task<(Question, Answer)> EditAnswer(string questionId, string answerId, string newContent)
    {
        Guard.IsNotNullOrWhiteSpace(questionId, nameof(questionId));
        Guard.IsNotNullOrWhiteSpace(answerId, nameof(answerId));
        Guard.IsNotNullOrWhiteSpace(newContent, nameof(newContent));
        
        var question = await this._documentSession.RequiredLoadAsync<Question>(questionId);

        var answer = question.Answers.Single(f => f.Id == answerId);
        var currentUser = await this._currentUserService.GetRequiredCurrentUser();

        // TODO: Allow moderators or admins to edit answers from other people
        if (answer.AnswerByUserId != currentUser.HelpoUserId)
            throw new Exception("You can only edit your own answers.");

        answer.Content = newContent;

        return (question, answer);
    }
}
