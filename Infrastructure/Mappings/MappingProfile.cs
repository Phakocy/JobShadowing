using AutoMapper;
using JobShadowing.Application.DTOs;
using JobShadowing.Application.DTOs.Auth;
using JobShadowing.Application.DTOs.Projects;
using JobShadowing.Application.DTOs.Teams;
using JobShadowing.Domain.Entities;

namespace JobShadowing.Infrastructure.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Task mappings
            CreateMap<TaskItem, TaskResponseDto>()
                .ForMember(dest => dest.StatusDisplay,
                    opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.IsOverdue,
                    opt => opt.MapFrom(src => src.DueDate.HasValue && src.DueDate.Value < DateTime.UtcNow));

            CreateMap<TaskItem, TaskSummaryDto>()
                .ForMember(dest => dest.IsOverdue,
                    opt => opt.MapFrom(src => src.DueDate.HasValue && src.DueDate.Value < DateTime.UtcNow));

            CreateMap<CreateTaskDto, TaskItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectId, opt => opt.Ignore())
                .ForMember(dest => dest.Project, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<UpdateTaskDto, TaskItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ProjectId, opt => opt.Ignore())
                .ForMember(dest => dest.Project, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // User mappings
            CreateMap<User, UserDto>();

            // Project mappings
            CreateMap<Project, ProjectDto>()
                .ForMember(dest => dest.TaskCount, opt => opt.MapFrom(src => src.Tasks.Count))
                .ForMember(dest => dest.Team, opt => opt.MapFrom(src => src.Team));

            CreateMap<Project, ProjectSummaryDto>()
                .ForMember(dest => dest.TaskCount, opt => opt.MapFrom(src => src.Tasks.Count))
                .ForMember(dest => dest.IsTeamProject, opt => opt.MapFrom(src => src.TeamId.HasValue));

            // Team mappings
            CreateMap<Team, TeamDto>()
                .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Members.Count))
                .ForMember(dest => dest.ProjectCount, opt => opt.MapFrom(src => src.Projects.Count));

            CreateMap<Team, TeamSummaryDto>();

            CreateMap<TeamMember, TeamMemberDto>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName));
        }
    }
}
