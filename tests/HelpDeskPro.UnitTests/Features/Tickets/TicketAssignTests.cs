namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using Xunit;

public class TicketAssignTests
{
    private readonly DateTime _utcNow = new(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc);
    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly Department _itDept = new() { Id = 1, Code = "IT", Name = "Information Technology", IsActive = true };

    private Ticket CreateOpenTicket()
    {
        var slaPolicy = new SlaPolicy
        {
            Id = 1,
            Priority = TicketPriority.Medium,
            ResponseTimeMinutes = 240,
            ResolutionTimeMinutes = 1440
        };

        return Ticket.Create(
            "HD-2026-000001",
            "Permintaan Dukungan IT",
            "Koneksi jaringan terputus di lantai 3",
            1,
            TicketPriority.Medium,
            _requesterId,
            slaPolicy,
            _utcNow,
            "requester@helpdeskpro.local");
    }

    [Fact]
    public void Assign_AgentSelfAssignUnassignedTicket_ShouldSucceedAndMoveToInProgress()
    {
        // Arrange
        var ticket = CreateOpenTicket();
        var agent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "agent1@helpdeskpro.local",
            FullName = "Agent IT Satu",
            Department = _itDept,
            DepartmentId = _itDept.Id
        };

        // Act
        ticket.Assign(agent, agent.Id, "Agent", _utcNow);

        // Assert
        ticket.AssigneeId.Should().Be(agent.Id);
        ticket.Assignee.Should().Be(agent);
        ticket.Status.Should().Be(TicketStatus.InProgress);
        ticket.FirstRespondedAt.Should().Be(_utcNow);

        ticket.Histories.Should().Contain(h =>
            h.FieldName == "Assignee" &&
            h.NewValue == agent.FullName &&
            h.ChangedById == agent.Id);

        ticket.Histories.Should().Contain(h =>
            h.FieldName == "Status" &&
            h.OldValue == TicketStatus.Open.ToString() &&
            h.NewValue == TicketStatus.InProgress.ToString() &&
            h.ChangedById == agent.Id);
    }

    [Fact]
    public void Assign_AgentAssignsToAnotherAgent_ThrowsBusinessRuleException()
    {
        // Arrange
        var ticket = CreateOpenTicket();
        var agentCallerId = Guid.NewGuid();
        var targetAgent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "target@helpdeskpro.local",
            FullName = "Target Agent",
            Department = _itDept,
            DepartmentId = _itDept.Id
        };

        // Act
        var act = () => ticket.Assign(targetAgent, agentCallerId, "Agent", _utcNow);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Agent hanya dapat mengambil tiket untuk dirinya sendiri.");
    }

    [Fact]
    public void Assign_AgentTakesAlreadyAssignedTicket_ThrowsBusinessRuleException()
    {
        // Arrange
        var ticket = CreateOpenTicket();
        var existingAgent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "first@helpdeskpro.local",
            FullName = "First Agent",
            Department = _itDept,
            DepartmentId = _itDept.Id
        };
        ticket.Assign(existingAgent, existingAgent.Id, "Supervisor", _utcNow);

        var secondAgent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "second@helpdeskpro.local",
            FullName = "Second Agent",
            Department = _itDept,
            DepartmentId = _itDept.Id
        };

        // Act
        var act = () => ticket.Assign(secondAgent, secondAgent.Id, "Agent", _utcNow);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Tiket sudah memiliki penangan dan tidak dapat diambil.");
    }

    [Fact]
    public void Assign_SupervisorAssignsTicketToAnyAgent_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateOpenTicket();
        var supervisorId = Guid.NewGuid();
        var agent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "agent@helpdeskpro.local",
            FullName = "Staff Agent IT",
            Department = _itDept,
            DepartmentId = _itDept.Id
        };

        // Act
        ticket.Assign(agent, supervisorId, "Supervisor", _utcNow);

        // Assert
        ticket.AssigneeId.Should().Be(agent.Id);
        ticket.Status.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public void Assign_AdminAssignsTicketToAnyAgent_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateOpenTicket();
        var adminId = Guid.NewGuid();
        var agent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "agent@helpdeskpro.local",
            FullName = "Staff Agent IT",
            Department = _itDept,
            DepartmentId = _itDept.Id
        };

        // Act
        ticket.Assign(agent, adminId, "Admin", _utcNow);

        // Assert
        ticket.AssigneeId.Should().Be(agent.Id);
        ticket.Status.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public void Assign_RequesterRoleAttemptsAssign_ThrowsBusinessRuleException()
    {
        // Arrange
        var ticket = CreateOpenTicket();
        var requesterCallerId = Guid.NewGuid();
        var agent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "agent@helpdeskpro.local",
            FullName = "Staff Agent IT",
            Department = _itDept,
            DepartmentId = _itDept.Id
        };

        // Act
        var act = () => ticket.Assign(agent, requesterCallerId, "Requester", _utcNow);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Role Anda tidak memiliki wewenang untuk menugaskan tiket.");
    }

    [Fact]
    public void Assign_ReassignTicketAlreadyInProgress_PreservesFirstRespondedAtAndDoesNotAddStatusHistory()
    {
        // Arrange
        var ticket = CreateOpenTicket();
        var agent1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "agent1@helpdeskpro.local",
            FullName = "Agent Satu",
            Department = _itDept,
            DepartmentId = _itDept.Id
        };
        var firstAssignTime = _utcNow;
        ticket.Assign(agent1, agent1.Id, "Agent", firstAssignTime);

        var supervisorId = Guid.NewGuid();
        var agent2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "agent2@helpdeskpro.local",
            FullName = "Agent Dua",
            Department = _itDept,
            DepartmentId = _itDept.Id
        };
        var reassignTime = firstAssignTime.AddHours(1);

        // Act
        ticket.Assign(agent2, supervisorId, "Supervisor", reassignTime);

        // Assert
        ticket.AssigneeId.Should().Be(agent2.Id);
        ticket.FirstRespondedAt.Should().Be(firstAssignTime);
        ticket.Status.Should().Be(TicketStatus.InProgress);

        // Hanya ada 1 history Status (saat assign pertama) dan 2 history Assignee
        ticket.Histories.Count(h => h.FieldName == "Status").Should().Be(1);
        ticket.Histories.Count(h => h.FieldName == "Assignee").Should().Be(2);
    }
}

