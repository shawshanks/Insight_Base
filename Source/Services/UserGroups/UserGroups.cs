﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Insight.Base.Common;
using Insight.Base.Common.Entity;
using Insight.Base.OAuth;
using Insight.Utils.Common;
using Insight.Utils.Entity;

namespace Insight.Base.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class UserGroups : ServiceBase, IUserGroups
    {
        /// <summary>
        /// 为跨域请求设置响应头信息
        /// </summary>
        public void responseOptions()
        {
        }

        /// <summary>
        /// 根据对象实体数据新增一个用户组
        /// </summary>
        /// <param name="group">用户组对象</param>
        /// <returns>Result</returns>
        public Result<object> addGroup(Group group)
        {
            if (!verify("newGroup")) return result;

            group.id = Util.newId();
            group.tenantId = tenantId;
            if (existed(group)) return result.dataAlreadyExists();

            group.creatorId = userId;
            group.createTime = DateTime.Now;

            return DbHelper.insert(group) ? result.created(group) : result.dataBaseError();
        }

        /// <summary>
        /// 根据ID删除用户组
        /// </summary>
        /// <param name="id">用户组ID</param>
        /// <returns>Result</returns>
        public Result<object> removeGroup(string id)
        {
            if (!verify("deleteGroup")) return result;

            var data = getData(id);
            if (data == null) return result.notFound();

            return DbHelper.delete(data) ? result : result.dataBaseError();
        }

        /// <summary>
        /// 根据对象实体数据更新用户组信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="group">用户组对象</param>
        /// <returns>Result</returns>
        public Result<object> updateGroup(string id, Group @group)
        {
            if (!verify("editGroup")) return result;

            var data = getData(group.id);
            if (data == null) return result.notFound();

            data.name = group.name;
            data.remark = group.remark;
            if (existed(data)) return result.dataAlreadyExists();

            return DbHelper.update(data) ? result : result.dataBaseError();
        }

        /// <summary>
        /// 根据ID获取用户组对象实体
        /// </summary>
        /// <param name="id">用户组ID</param>
        /// <returns>Result</returns>
        public Result<object> getGroup(string id)
        {
            if (!verify("getGroups")) return result;

            var data = getData(id);
            return data == null ? result.notFound() : result.success(data);
        }

        /// <summary>
        /// 获取全部用户组
        /// </summary>
        /// <param name="rows">每页行数</param>
        /// <param name="page">当前页</param>
        /// <param name="key">查询关键词</param>
        /// <returns>Result</returns>
        public Result<object> getGroups(int rows, int page, string key)
        {
            if (!verify("getGroups")) return result;

            if (page < 1 || rows > 100) return result.badRequest();

            using (var context = new Entities())
            {
                var skip = rows * (page - 1);
                var list = context.groups.Where(i => i.tenantId == tenantId && (string.IsNullOrEmpty(key) || i.name.Contains(key) || i.remark.Contains(key)));
                var data = list.OrderBy(i => i.createTime).Skip(skip).Take(rows).ToList();

                return result.success(data, list.Count());
            }
        }

        /// <summary>
        /// 根据参数组集合批量插入用户组成员关系
        /// </summary>
        /// <param name="id"></param>
        /// <param name="group">UserGroup</param>
        /// <returns>Result</returns>
        public Result<object> addGroupMember(string id, Group @group)
        {
            if (!verify("addGroupMember")) return result;

            var data = getData(group.id);
            if (data == null) return result.notFound();

            group.members.ForEach(i =>
            {
                i.groupId = group.id;
                i.creatorId = userId;
                i.createTime = DateTime.Now;
            });

            return DbHelper.insert(group.members) ? result.success(group) : result.dataBaseError();
        }

        /// <summary>
        /// 根据ID集合删除用户组成员关系
        /// </summary>
        /// <param name="id"></param>
        /// <param name="group">UserGroup</param>
        /// <returns>Result</returns>
        public Result<object> removeMember(string id, Group @group)
        {
            if (!verify("removeGroupMember")) return result;

            var data = getData(group.id);
            if (data == null) return result.notFound();

            return DbHelper.delete(group.members) ? result.success(group) : result.dataBaseError();
        }

        /// <summary>
        /// 根据ID获取组成员之外的全部用户
        /// </summary>
        /// <param name="id">用户组ID</param>
        /// <returns>Result</returns>
        public Result<object> getOtherUser(string id)
        {
            if (!verify("getGroups")) return result;

            using (var context = new Entities())
            {
                var list = from u in context.users
                           join r in context.tenantUsers.Where(i => i.tenantId == tenantId) on u.id equals r.userId
                           join m in context.groupMembers.Where(i => i.groupId == id) on u.id equals m.userId
                           into temp from t in temp.DefaultIfEmpty()
                           where t == null && !u.isInvalid
                           orderby u.createTime
                           select new {u.id, u.name, u.account};
                return list.Any() ? result.success(list.ToList()) : result.noContent(new List<object>());
            }
        }

        /// <summary>
        /// 获取指定ID的用户组
        /// </summary>
        /// <param name="id">用户组ID</param>
        /// <returns>用户组</returns>
        private static Group getData(string id)
        {
            using (var context = new Entities())
            {
                return context.groups.SingleOrDefault(i => i.id == id);
            }
        }

        /// <summary>
        /// 用户组是否已存在
        /// </summary>
        /// <param name="group">用户组</param>
        /// <returns>是否已存在</returns>
        private static bool existed(Group group)
        {
            using (var context = new Entities())
            {
                return context.groups.Any(i => i.id != group.id && i.tenantId == group.tenantId && i.name == group.name);
            }
        }
    }
}