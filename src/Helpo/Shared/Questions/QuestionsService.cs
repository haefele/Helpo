using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Helpo.Services;
using Helpo.Shared.Auth;
using Raven.Client.Documents.Session;

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

    public async Task<Question> AskQuestion(string title, string content)
    {
        Guard.IsNotNullOrWhiteSpace(title, nameof(title));
        Guard.IsNotNullOrWhiteSpace(content, nameof(content));

        var currentUser = await this._currentUserService.GetRequiredCurrentUser();

        var question = new Question 
        {
            Id = this._idFactory.GenerateId(),
            Title = title,
            Content = content,
            AskedAt = DateTimeOffset.Now,
            AskedByUserId = currentUser.HelpoUserId
        };

        await this._documentSession.StoreAsync(question);

        return question;
    }

    public async Task<(Question, Answer)> AddAnswer(string questionId, string content)
    {
        Guard.IsNotNullOrWhiteSpace(questionId, nameof(questionId));
        Guard.IsNotNullOrWhiteSpace(content, nameof(content));
        
        var question = await this._documentSession.LoadAsync<Question>(questionId);

        if (question is null)
            throw new Exception("No question found with id.");

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

    public async Task<(Question, Answer)> EditAnswer(string questionId, string answerId, string content)
    {
        Guard.IsNotNullOrWhiteSpace(questionId, nameof(questionId));
        Guard.IsNotNullOrWhiteSpace(answerId, nameof(answerId));
        Guard.IsNotNullOrWhiteSpace(content, nameof(content));
        
        var question = await this._documentSession.LoadAsync<Question>(questionId);
        if (question is null)
            throw new Exception("No question found with id.");

        var answer = question.Answers.FirstOrDefault(f => f.Id == answerId);
        if (answer is null)
            throw new Exception("No answer found with id.");

        var currentUser = await this._currentUserService.GetRequiredCurrentUser();

        // TODO: Allow moderators or admins to edit answers from other people
        if (answer.AnswerByUserId != currentUser.HelpoUserId)
            throw new Exception("You can only edit your own answers.");

        answer.Content = content;

        return (question, answer);
    }
}
