﻿// -----------------------------------------------------------------------
//  <copyright file="IdentityService.User.cs" company="OSharp开源团队">
//      Copyright (c) 2014-2015 OSharp. All rights reserved.
//  </copyright>
//  <last-editor>郭明锋</last-editor>
//  <last-date>2015-03-24 17:25</last-date>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Linq.Expressions;

using OSharp.Demo.Dtos.Identity;
using OSharp.Demo.Models.Identity;
using OSharp.Utility.Data;
using OSharp.Utility;
using OSharp.Utility.Extensions;


namespace OSharp.Demo.Services
{
    public partial class IdentityService
    {
        #region Implementation of IIdentityContract

        /// <summary>
        /// 获取 用户信息查询数据集
        /// </summary>
        public IQueryable<User> Users
        {
            get { return UserRepository.Entities; }
        }

        /// <summary>
        /// 检查用户信息信息是否存在
        /// </summary>
        /// <param name="predicate">检查谓语表达式</param>
        /// <param name="id">更新的用户信息编号</param>
        /// <returns>用户信息是否存在</returns>
        public bool CheckUserExists(Expression<Func<User, bool>> predicate, int id = 0)
        {
            return UserRepository.CheckExists(predicate, id);
        }

        /// <summary>
        /// 添加用户信息信息
        /// </summary>
        /// <param name="dtos">要添加的用户信息DTO信息</param>
        /// <returns>业务操作结果</returns>
        public OperationResult AddUsers(params UserDto[] dtos)
        {
            return UserRepository.Insert(dtos,
                (dto) =>
                {
                    if (UserRepository.CheckExists(u => u.Name == dto.Name))
                    {
                        throw new Exception("名字为“{0}”的用户已存在，不能重复添加。".FormatWith(dto.Name));
                    }
                },
                (dto, entity) =>
                {
                    UserExtendRepository.Insert(new UserExtend() { RegistedIp = dto.RegistedIp, User = entity });
                    var userExtends = UserExtendRepository.Entities.Where(extend => extend.User.Name == entity.Name);

                    if (userExtends.Count() != 1)
                    {
                        throw new Exception("用户{0}的扩展信息获取异常.".FormatWith(entity.Name));
                    }

                    entity.Extend = userExtends.First();

                    return entity;
                });
        }

        /// <summary>
        /// 更新用户信息信息
        /// </summary>
        /// <param name="dtos">包含更新信息的用户信息DTO信息</param>
        /// <returns>业务操作结果</returns>
        public OperationResult EditUsers(params UserDto[] dtos)
        {
            return UserRepository.Update(dtos, dto =>
            {
                if (!UserRepository.CheckExists(u => u.Name == dto.Name))
                {
                    throw new Exception("名字为“{0}”的用户不存在，无法进行更新。".FormatWith(dto.Name));
                }
            },
           (dto, entity) =>
           {
               var user = UserRepository.GetByKey(entity.Id);

               if (user == null)
               {
                   throw new Exception("要更新的用户{0}不存在.".FormatWith(entity.Name));
               }

               user.Extend.RegistedIp = dto.RegistedIp;

               return entity;
           });
        }

        /// <summary>
        /// 删除用户信息信息
        /// </summary>
        /// <param name="ids">要删除的用户信息编号</param>
        /// <returns>业务操作结果</returns>
        public OperationResult DeleteUsers(params int[] ids)
        {
            ids.CheckNotNull("ids");

            return UserRepository.Delete(ids,
                entity =>
                {
                    var user = UserRepository.GetByKey(entity.Id);
                    if (user == null)
                    {
                        throw new Exception("用户{0}不存在。".FormatWith(entity.Name));
                    }
                },
                entity =>
                {
                    UserExtendRepository.Delete(entity.Extend.Id);

                    return entity;
                });
        }

        /// <summary>
        /// 设置用户的角色
        /// </summary>
        /// <param name="id">用户编号</param>
        /// <param name="roleIds">角色编号集合</param>
        /// <returns>业务操作结果</returns>
        public OperationResult SetUserRoles(int id, int[] roleIds)
        {
            roleIds.CheckNotNull("roleIds");

            var userDtos = new System.Collections.Generic.List<UserDto>() { new UserDto { Id = id } };

            return UserRepository.Update(userDtos,
                dto =>
                {
                    var user = UserRepository.GetByKey(id);

                    if (user == null)
                    {
                        throw new Exception("用户id:{0}不存在.".FormatWith(id));
                    }
                },
                (dto, entity) =>
                {
                    foreach (var roleId in roleIds)
                    {
                        var role = RoleRepository.GetByKey(roleId);
                        //if(role==null)  是不是过度了？
                        entity.Roles.Add(role);
                    }

                    return entity;
                });
        }

        #endregion
    }
}