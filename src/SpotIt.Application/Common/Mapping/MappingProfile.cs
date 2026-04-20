using AutoMapper;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Entities;

namespace SpotIt.Application.Common.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Post, PostDto>()
            .ForMember(dest => dest.LikesCount, opt => opt.MapFrom(src => src.Likes.Count))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.IsAnonymous ? null : src.AuthorId))
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.IsAnonymous ? null : src.Author.UserName));
    }
}
