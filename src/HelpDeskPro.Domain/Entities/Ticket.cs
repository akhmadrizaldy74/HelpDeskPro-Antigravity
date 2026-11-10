namespace HelpDeskPro.Domain.Entities;

using HelpDeskPro.Domain.Common;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;

public class Ticket : AuditableEntity<long>
{
    private static readonly Dictionary<TicketStatus, HashSet<TicketStatus>> _allowedTransitions = new()
    {
        [TicketStatus.Open] = [TicketStatus.InProgress, TicketStatus.Cancelled],
        [TicketStatus.InProgress] = [TicketStatus.Pending, TicketStatus.Resolved],
        [TicketStatus.Pending] = [TicketStatus.InProgress],
        [TicketStatus.Resolved] = [TicketStatus.Closed, TicketStatus.InProgress],
        [TicketStatus.Closed] = [],
        [TicketStatus.Cancelled] = []
    };

    public string TicketNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; }
    public Guid RequesterId { get; set; }
    public Guid? AssigneeId { get; set; }
    public int SlaPolicyId { get; set; }
    public DateTime ResponseDueAt { get; set; }
    public DateTime ResolutionDueAt { get; set; }
    public DateTime? FirstRespondedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public int TotalPausedMinutes { get; set; }
    public bool IsSlaBreached { get; set; }
    public byte EscalationLevel { get; set; }
    public int? Rating { get; set; }
    public string? RatingComment { get; set; }
    public DateTime? RatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Category Category { get; set; } = null!;
    public ApplicationUser Requester { get; set; } = null!;
    public ApplicationUser? Assignee { get; set; }
    public SlaPolicy SlaPolicy { get; set; } = null!;

    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketHistory> Histories { get; set; } = new List<TicketHistory>();
    public ICollection<Escalation> Escalations { get; set; } = new List<Escalation>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public static Ticket Create(
        string ticketNo,
        string title,
        string? description,
        int categoryId,
        TicketPriority priority,
        Guid requesterId,
        SlaPolicy slaPolicy,
        DateTime utcNow,
        string? createdBy = null,
        DateTime? responseDueAt = null,
        DateTime? resolutionDueAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticketNo);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(slaPolicy);

        var ticket = new Ticket
        {
            TicketNo = ticketNo.Trim(),
            Title = title.Trim(),
            Description = description?.Trim(),
            CategoryId = categoryId,
            Priority = priority,
            Status = TicketStatus.Open,
            RequesterId = requesterId,
            SlaPolicyId = slaPolicy.Id,
            SlaPolicy = slaPolicy,
            ResponseDueAt = responseDueAt ?? utcNow.AddMinutes(slaPolicy.ResponseTimeMinutes),
            ResolutionDueAt = resolutionDueAt ?? utcNow.AddMinutes(slaPolicy.ResolutionTimeMinutes),
            FirstRespondedAt = null,
            ResolvedAt = null,
            ClosedAt = null,
            PausedAt = null,
            TotalPausedMinutes = 0,
            IsSlaBreached = false,
            EscalationLevel = 0,
            CreatedAt = utcNow,
            CreatedBy = createdBy
        };

        ticket.Histories.Add(new TicketHistory
        {
            FieldName = "Ticket",
            OldValue = null,
            NewValue = "Created",
            ChangedById = requesterId,
            ChangedAt = utcNow
        });

        return ticket;
    }

    public void ChangeStatus(
        TicketStatus newStatus,
        string? reason,
        Guid? changedById,
        string userRole,
        DateTime utcNow,
        DateTime? adjustedResolutionDueAt = null,
        int? workingPausedMinutes = null)
    {
        // 1. Check if transition is in the allowed transition table
        if (!_allowedTransitions.TryGetValue(Status, out var targetStatuses) || !targetStatuses.Contains(newStatus))
        {
            throw new BusinessRuleException($"Perubahan status dari {Status} ke {newStatus} tidak diizinkan.");
        }

        // 2. Role-specific restrictions
        if (userRole == "Requester")
        {
            var isAllowedForRequester = (Status == TicketStatus.Open && newStatus == TicketStatus.Cancelled)
                                     || (Status == TicketStatus.Resolved && newStatus == TicketStatus.Closed);

            if (!isAllowedForRequester)
            {
                throw new BusinessRuleException($"Pengguna dengan role Requester tidak diizinkan mengubah status dari {Status} ke {newStatus}.");
            }
        }

        // 3. Reopen restriction: Resolved -> InProgress allowed within 7 days of ResolvedAt
        if (Status == TicketStatus.Resolved && newStatus == TicketStatus.InProgress)
        {
            if (ResolvedAt.HasValue && (utcNow - ResolvedAt.Value).TotalDays > 7)
            {
                throw new BusinessRuleException("Tiket yang sudah selesai lebih dari 7 hari tidak dapat dibuka ulang.");
            }
        }

        // 4. Pending requirement: reason is required
        if (newStatus == TicketStatus.Pending)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new BusinessRuleException("Alasan penundaan tiket wajib diisi.");
            }

            if (!changedById.HasValue)
            {
                throw new BusinessRuleException("Pengguna yang mengubah status ke Pending wajib ditentukan.");
            }

            // FR-13: Pindah ke Pending wajib menyertakan alasan (disimpan sebagai komentar internal)
            Comments.Add(new TicketComment
            {
                AuthorId = changedById.Value,
                Body = reason.Trim(),
                IsInternal = true,
                CreatedAt = utcNow
            });

            // Pause SLA: masuk Pending isi PausedAt
            PausedAt = utcNow;
        }

        // 5. Resolved requirement: completion note is required
        if (newStatus == TicketStatus.Resolved)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new BusinessRuleException("Catatan penyelesaian tiket wajib diisi.");
            }

            ResolvedAt = utcNow;
        }

        // 6. Resume SLA: keluar dari Pending
        if (Status == TicketStatus.Pending && newStatus != TicketStatus.Pending)
        {
            if (PausedAt.HasValue)
            {
                var pausedMinutes = workingPausedMinutes ?? (int)Math.Round((utcNow - PausedAt.Value).TotalMinutes);
                if (pausedMinutes > 0)
                {
                    TotalPausedMinutes += pausedMinutes;
                    ResolutionDueAt = adjustedResolutionDueAt ?? ResolutionDueAt.AddMinutes(pausedMinutes);
                }
                PausedAt = null;
            }
        }

        // 7. Closed
        if (newStatus == TicketStatus.Closed)
        {
            ClosedAt ??= utcNow;
        }

        var oldStatus = Status;
        Status = newStatus;

        // 8. Record history
        Histories.Add(new TicketHistory
        {
            FieldName = "Status",
            OldValue = oldStatus.ToString(),
            NewValue = newStatus.ToString(),
            ChangedById = changedById,
            ChangedAt = utcNow
        });
    }

    public void Assign(
        ApplicationUser assignee,
        Guid assignedById,
        string userRole,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(assignee);

        if (userRole == "Agent")
        {
            if (assignee.Id != assignedById)
            {
                throw new BusinessRuleException("Agent hanya dapat mengambil tiket untuk dirinya sendiri.");
            }

            if (AssigneeId != null)
            {
                throw new BusinessRuleException("Tiket sudah memiliki penangan dan tidak dapat diambil.");
            }
        }
        else if (userRole != "Supervisor" && userRole != "Admin")
        {
            throw new BusinessRuleException("Role Anda tidak memiliki wewenang untuk menugaskan tiket.");
        }

        var oldAssigneeName = Assignee?.FullName;
        AssigneeId = assignee.Id;
        Assignee = assignee;

        // First assignment logic (FR-11):
        // Assign pertama kali mengisi FirstRespondedAt jika belum terisi, dan otomatis mengubah status Open -> InProgress.
        FirstRespondedAt ??= utcNow;

        if (Status == TicketStatus.Open)
        {
            Status = TicketStatus.InProgress;

            Histories.Add(new TicketHistory
            {
                FieldName = "Status",
                OldValue = TicketStatus.Open.ToString(),
                NewValue = TicketStatus.InProgress.ToString(),
                ChangedById = assignedById,
                ChangedAt = utcNow
            });
        }

        Histories.Add(new TicketHistory
        {
            FieldName = "Assignee",
            OldValue = oldAssigneeName,
            NewValue = assignee.FullName,
            ChangedById = assignedById,
            ChangedAt = utcNow
        });
    }

    public TicketComment AddComment(
        string body,
        bool isInternal,
        Guid authorId,
        string userRole,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new BusinessRuleException("Isi komentar tidak boleh kosong.");
        }

        if (body.Length > 4000)
        {
            throw new BusinessRuleException("Isi komentar melebihi batas maksimal 4000 karakter.");
        }

        // FR-16 & Matrix 3.1: Requester cannot create internal comments
        if (userRole == "Requester" && isInternal)
        {
            throw new BusinessRuleException("Requester tidak diizinkan membuat komentar internal.");
        }

        // FR-17: Komentar pertama dari Agent / Supervisor mengisi FirstRespondedAt jika belum terisi
        if (userRole is "Agent" or "Supervisor" or "Admin")
        {
            FirstRespondedAt ??= utcNow;
        }

        var comment = new TicketComment
        {
            TicketId = Id,
            AuthorId = authorId,
            Body = body.Trim(),
            IsInternal = isInternal,
            CreatedAt = utcNow
        };

        Comments.Add(comment);
        return comment;
    }

    public TicketAttachment AddAttachment(
        string fileName,
        string contentType,
        long fileSizeBytes,
        string storagePath,
        Guid uploadedById,
        DateTime utcNow)
    {
        // FR-18: Maks 5 file per tiket
        if (Attachments.Count >= 5)
        {
            throw new BusinessRuleException("Jumlah lampiran untuk tiket ini sudah mencapai batas maksimal (5 file).");
        }

        // FR-18: Maks 5 MB per file (5 * 1024 * 1024 = 5,242,880 bytes)
        if (fileSizeBytes > 5 * 1024 * 1024)
        {
            throw new BusinessRuleException("Ukuran file melebihi batas maksimal 5 MB.");
        }

        var attachment = new TicketAttachment
        {
            TicketId = Id,
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            StoragePath = storagePath,
            UploadedById = uploadedById,
            UploadedAt = utcNow
        };

        Attachments.Add(attachment);
        return attachment;
    }

    public void UpdateDetails(
        string title,
        string? description,
        Category category,
        TicketPriority priority,
        Guid changedById,
        string userRole,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new BusinessRuleException("Judul tiket tidak boleh kosong.");
        }

        if (title.Length < 5 || title.Length > 200)
        {
            throw new BusinessRuleException("Judul tiket harus antara 5 dan 200 karakter.");
        }

        if (description != null && description.Length > 4000)
        {
            throw new BusinessRuleException("Deskripsi tiket melebihi batas maksimal 4000 karakter.");
        }

        // Access matrix FSD 3.1:
        // Requester: Milik sendiri, status Open
        // Agent: Tidak
        // Supervisor: Ya
        // Admin: Ya
        if (userRole == "Agent")
        {
            throw new BusinessRuleException("Agent tidak memiliki wewenang untuk mengubah detail tiket.");
        }

        if (userRole == "Requester")
        {
            if (Status != TicketStatus.Open)
            {
                throw new BusinessRuleException("Requester hanya dapat mengubah tiket saat status masih Open.");
            }
        }
        else if (userRole != "Supervisor" && userRole != "Admin")
        {
            throw new BusinessRuleException("Role Anda tidak memiliki wewenang untuk mengubah detail tiket.");
        }

        // FR-19: Hanya catat riwayat untuk field yang berubah
        var trimmedTitle = title.Trim();
        if (Title != trimmedTitle)
        {
            Histories.Add(new TicketHistory
            {
                FieldName = "Title",
                OldValue = Title,
                NewValue = trimmedTitle,
                ChangedById = changedById,
                ChangedAt = utcNow
            });
            Title = trimmedTitle;
        }

        var trimmedDesc = description?.Trim();
        if (Description != trimmedDesc)
        {
            Histories.Add(new TicketHistory
            {
                FieldName = "Description",
                OldValue = Description,
                NewValue = trimmedDesc,
                ChangedById = changedById,
                ChangedAt = utcNow
            });
            Description = trimmedDesc;
        }

        if (CategoryId != category.Id)
        {
            var oldCategoryName = Category?.Name;
            Histories.Add(new TicketHistory
            {
                FieldName = "Category",
                OldValue = oldCategoryName,
                NewValue = category.Name,
                ChangedById = changedById,
                ChangedAt = utcNow
            });
            CategoryId = category.Id;
            Category = category;
        }

        if (Priority != priority)
        {
            Histories.Add(new TicketHistory
            {
                FieldName = "Priority",
                OldValue = Priority.ToString(),
                NewValue = priority.ToString(),
                ChangedById = changedById,
                ChangedAt = utcNow
            });
            // Perubahan prioritas TIDAK mengubah SLA due date yang sudah ada
            Priority = priority;
        }
    }

    public void Rate(
        int rating,
        string? comment,
        Guid ratedById,
        string userRole,
        DateTime utcNow)
    {
        if (Status != TicketStatus.Resolved)
        {
            throw new BusinessRuleException($"Rating hanya dapat diberikan pada tiket yang berstatus Resolved. Status tiket saat ini: {Status}.");
        }

        if (Rating.HasValue)
        {
            throw new BusinessRuleException("Tiket ini sudah pernah diberi rating kepuasan.");
        }

        if (userRole == "Requester" && RequesterId != ratedById)
        {
            throw new BusinessRuleException("Hanya pemohon tiket yang dapat memberikan rating kepuasan.");
        }
        else if (userRole != "Requester" && userRole != "Admin")
        {
            throw new BusinessRuleException("Role Anda tidak memiliki wewenang untuk memberikan rating kepuasan.");
        }

        if (rating < 1 || rating > 5)
        {
            throw new BusinessRuleException("Rating kepuasan harus bernilai antara 1 dan 5.");
        }

        var trimmedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (trimmedComment != null && trimmedComment.Length > 500)
        {
            throw new BusinessRuleException("Komentar rating tidak boleh melebihi 500 karakter.");
        }

        Rating = rating;
        RatingComment = trimmedComment;
        RatedAt = utcNow;

        var oldStatus = Status;
        Status = TicketStatus.Closed;
        ClosedAt = utcNow;

        Histories.Add(new TicketHistory
        {
            FieldName = "Rating",
            OldValue = null,
            NewValue = $"{rating}/5",
            ChangedById = ratedById,
            ChangedAt = utcNow
        });

        Histories.Add(new TicketHistory
        {
            FieldName = "Status",
            OldValue = oldStatus.ToString(),
            NewValue = TicketStatus.Closed.ToString(),
            ChangedById = ratedById,
            ChangedAt = utcNow
        });
    }
}
