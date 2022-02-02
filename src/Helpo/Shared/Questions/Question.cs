using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpo.Data;

namespace Helpo.Shared.Questions;

public class Question : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public List<string> Tags { get; set; } = new();

    public string AskedByUserId { get; set; } = default!;
    public DateTimeOffset AskedAt { get; set; } = default!;

    public List<Answer> Answers { get; set; } = new();
    public string? AcceptedAnswerId { get; set; }
}

public class Answer : EmbeddedEntity
{
    public string Content { get; set; } = default!;
    
    public string AnswerByUserId { get; set; } = default!;
    public DateTimeOffset AnsweredAt { get; set; } = default!;
}
