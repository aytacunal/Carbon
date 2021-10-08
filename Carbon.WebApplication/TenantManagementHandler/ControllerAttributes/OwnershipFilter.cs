﻿using Carbon.Common.TenantManagementHandler.Classes;
using Carbon.WebApplication.TenantManagementHandler.Dtos;
using Carbon.WebApplication.TenantManagementHandler.Extensions;
using Carbon.WebApplication.TenantManagementHandler.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Carbon.WebApplication.TenantManagementHandler.Interfaces;
using System.IO;
using Newtonsoft.Json;
using Carbon.Common;
using Microsoft.AspNetCore.Mvc;
using Carbon.ExceptionHandling.Abstractions;
using Carbon.WebApplication.TenantManagementHandler.Middlewares;

namespace Carbon.WebApplication.TenantManagementHandler.ControllerAttributes
{
    public enum OwnershipType
    {
        User = 0,
        Admin = 1,
        Superadmin = 2,
        Goduser = 99
    }

    public class OwnershipFilter : ActionFilterAttribute
    {
        private readonly string _role;
        private readonly OwnershipType _ownershipType;

        public OwnershipFilter(string role = null)
        {
            _role = role;
            _ownershipType = OwnershipType.User;
        }

        public OwnershipFilter(OwnershipType ownershipType = OwnershipType.User, string role = null)
        {
            _role = role;
            _ownershipType = ownershipType;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var _externalService = context.HttpContext.RequestServices.GetService<IExternalService>();
            var solutions = context.GetSolutionFilter();
            var userId = ((CarbonController)context.Controller).User.GetUserId();
            var tenantId = ((CarbonController)context.Controller).User.GetTenantId();
            var organizationId = ((CarbonController)context.Controller).User.GetOrganizationId();
            var isGodUser = ((CarbonController)context.Controller).User.CheckIfGodUser();
            context.HttpContext.Request.Headers.TryGetValue("Authorization", out StringValues daToken);

            List<PermissionDetailedDto> filterOwnershipPermissionList;

            if (_ownershipType == OwnershipType.Goduser && !isGodUser)
                throw new ForbiddenOperationException(CarbonExceptionMessages.OnlyGodUserOperation);

            if (isGodUser)
                return;

            if (solutions == null || !solutions.Any())
                throw new ForbiddenOperationException(CarbonExceptionMessages.SolutionHeaderMustBeSet);


            filterOwnershipPermissionList = _externalService.ExecuteInPolicyApi_GetRoles(new PermissionDetailedFilterDto() { TenantId = tenantId, UserPolicyId = organizationId, UserId = userId, SolutionId = solutions.FirstOrDefault(), PermissionNames = new List<string>() { _role } }, daToken.ToString().Split(' ')[1]).Result;
            RoleExtensions.SetPermissions(filterOwnershipPermissionList);

            if (_ownershipType == OwnershipType.Admin)
                if (filterOwnershipPermissionList == null || !(filterOwnershipPermissionList.Where(k => k.OriginatedRoleType <= 2).Any()))
                {
                    throw new ForbiddenOperationException(CarbonExceptionMessages.OnlyAdminUserOperation);
                }

            if (_ownershipType == OwnershipType.Superadmin)
                if (filterOwnershipPermissionList == null || !(filterOwnershipPermissionList.Where(k => k.OriginatedRoleType == 1).Any()))
                {
                    throw new ForbiddenOperationException(CarbonExceptionMessages.OnlySuperAdminUserOperation);
                }

            if (filterOwnershipPermissionList != null && filterOwnershipPermissionList.Any())
            {
                var relatedController = ((IOwnershipFilteredController)context.Controller);
                relatedController.OwnershipFilteredServices.ForEach(k => k.SetFilter(filterOwnershipPermissionList));
                if (BodyRewindSettings.Enabled)
                {
                    context.HttpContext.Request.Body.Position = 0;
                    using (var reader = new StreamReader(context.HttpContext.Request.Body))
                    {
                        RoleFilteredConcreteDto reqBody = null;
                        var body = reader.ReadToEndAsync().Result;
                        try
                        {
                            try
                            {
                                reqBody = JsonConvert.DeserializeObject<RoleFilteredConcreteDto>(body);
                            }
                            catch
                            {
                                //Unable to parse then skip
                            }
                            if (reqBody != null && reqBody.OwnerType != OwnerType.None)
                            {
                                if (!reqBody.ValidateFilter(filterOwnershipPermissionList))
                                {
                                    throw new ForbiddenOperationException();
                                }
                            }
                        }
                        catch (ForbiddenOperationException ex)
                        {
                            throw ex;
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        finally
                        {
                            context.HttpContext.Request.Body.Dispose();
                        }
                    }
                }
            }
            else
            {
                context.HttpContext.Response.StatusCode = 403;
                context.Result = new JsonResult(new { response = "No_Permission", message = _role + " permission not found!" });
            }

        }


    }
}
