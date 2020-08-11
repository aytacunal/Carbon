﻿using Carbon.Common;
using Carbon.PagedList;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Carbon.WebApplication
{

    public abstract class CarbonController : ControllerBase
    {

        [ApiExplorerSettings(IgnoreApi = true)]
        protected ObjectResult ResponseResult<T>(T value) where T : IApiResponse
        {
            var httpStatusCode = value.StatusCode.GetHttpStatusCode();
            return StatusCode((int)httpStatusCode, value);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected ObjectResult ResponseConflict<T>(T value)
        {
            var result = new ApiResponse<T>(GetRequestIdentifier(), ApiStatusCode.Conflict);
            result.SetData(value);

            return ResponseResult(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected ObjectResult ResponseNotFound<T>(T value)
        {
            var result = new ApiResponse<T>(GetRequestIdentifier(), ApiStatusCode.NotFound);
            result.SetData(value);

            return ResponseResult(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected OkObjectResult ResponseOk<T>(T value)
        {
            var result = new ApiResponse<T>(GetRequestIdentifier(), ApiStatusCode.OK);
            result.SetData(value);

            return Ok(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected OkObjectResult ResponseOk()
        {
            var result = new ApiResponse<object>(GetRequestIdentifier(), ApiStatusCode.OK);
            return Ok(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected CreatedAtActionResult UpdatedOk<T>(string actionName, object routeValues, T value)
        {
            var result = new ApiResponse<T>(GetRequestIdentifier(), ApiStatusCode.OK);
            result.SetData(value);

            return CreatedAtAction(actionName, routeValues, result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected OkObjectResult DeletedOk()
        {
            var result = new ApiResponse<object>(GetRequestIdentifier(), ApiStatusCode.OK);
            return Ok(result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected CreatedAtActionResult CreatedOk<T>(string actionName, object routeValues, T value)
        {
            var result = new ApiResponse<T>(GetRequestIdentifier(), ApiStatusCode.OK);
            result.SetData(value);

            return CreatedAtAction(actionName, routeValues, result);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected OkObjectResult PagedListOk<T>(IPagedList<T> entity)
        {
            IPagedList pageable = entity;
            Response.Headers.Add("X-Paging-PageIndex", pageable.PageNumber.ToString());
            Response.Headers.Add("X-Paging-PageSize", pageable.PageSize.ToString());
            Response.Headers.Add("X-Paging-PageCount", pageable.PageCount.ToString());
            Response.Headers.Add("X-Paging-TotalRecordCount", pageable.TotalItemCount.ToString());

            IOrderableDto orderableDto = null;
            if (typeof(IOrderableDto).IsAssignableFrom(typeof(T)))
            {
                orderableDto = (IOrderableDto)entity;
            }

            if (orderableDto == null)
            {
                if (pageable.PageNumber > 1)
                {
                    AddParameter("X-Paging-Previous-Link", null, pageable.PageSize, pageable.PageNumber - 1);
                }
                if (pageable.PageNumber * pageable.PageSize < pageable.TotalItemCount)
                {
                    AddParameter("X-Paging-Next-Link", null, pageable.PageSize, pageable.PageNumber + 1);
                }
            }
            else
            {
                if (pageable.PageNumber > 1)
                {
                    AddParameter("X-Paging-Previous-Link", orderableDto.Orderables, pageable.PageSize, pageable.PageNumber - 1);
                }
                if (pageable.PageNumber * pageable.PageSize < pageable.PageNumber)
                {
                    AddParameter("X-Paging-Next-Link", orderableDto.Orderables, pageable.PageSize, pageable.PageNumber + 1);
                }
            }

            var result = new ApiResponse<IPagedList<T>>(GetRequestIdentifier(), ApiStatusCode.OK);
            result.SetData(entity);

            return Ok(result);
        }

        protected void AddParameter(string key, IList<Orderable> ordination, int pageSize, int pageIndex)
        {
            var builder = new StringBuilder();

            builder.Append("?");
            if (ordination != null)
            {
                for (int i = 0; i < ordination.Count; i++)
                {
                    if (builder.Length > 1)
                        builder.Append("&");

                    var value = ordination[i].Value;
                    var isAscending = ordination[i].IsAscending;

                    builder.Append(nameof(ordination)).Append("[").Append(i).Append("]").Append(".")
                           .Append(nameof(value)).Append("=").Append(value)
                           .Append("&")
                           .Append(nameof(ordination)).Append("[").Append(i).Append("]").Append(".")
                           .Append(nameof(isAscending)).Append("=").Append(isAscending.ToString());
                }
            }

            if (builder.Length > 1)
                builder.Append("&");

            var headerParameterLink = builder.Append(nameof(pageSize)).Append("=").Append(pageSize)
                                      .Append("&")
                                      .Append(nameof(pageIndex)).Append("=").Append(pageIndex)
                                      .ToString();

            headerParameterLink = $"{Request.Scheme}://{Request.Host}{Request.Path}{headerParameterLink}";
            Response.Headers.Add(key, headerParameterLink);
        }

        private string GetRequestIdentifier()
        {
            if (Request != null && Request.Headers != null)
            {
                if(Request.Headers.TryGetValue("X-CorrelationId", out var xCorrelationId))
                {
                    return xCorrelationId;
                }
                else if (Request.Headers.TryGetValue("correlationId", out var correlationId))
                {
                    return correlationId;
                }
            }

            return Guid.NewGuid().ToString();
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Obsolete("This method is obsolete. Usage for only apis dependant to P360Controller")]
        protected OkObjectResult PagedOk<T>(T TEntity, OrdinatedPageDto ordinatedPageDto, int totalCount)
        {
            if (ordinatedPageDto.PageIndex > 1)
            {
                Response.Headers.AddParameter(Request, "X-Paging-Previous-Link", ordinatedPageDto.Ordination, ordinatedPageDto.PageSize, ordinatedPageDto.PageIndex - 1);
            }
            if (ordinatedPageDto.PageIndex < totalCount)
            {
                Response.Headers.AddParameter(Request, "X-Paging-Next-Link", ordinatedPageDto.Ordination, ordinatedPageDto.PageSize, ordinatedPageDto.PageIndex + 1);
            }
            return Ok(TEntity);
        }
    }

    [Obsolete("This class is obsolete. Usage for only apis dependant to P360Controller")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class OrdinatedPageDto
    {
        public List<Ordination> Ordination { get; set; }
        public int PageSize { get; set; }
        public int PageIndex { get; set; }

        public OrdinatedPageDto()
        {
            PageSize = 250;
            PageIndex = 1;
            //Ordination = new List<Ordination>() { new Common.Ordination() {IsAscending = false, Value = "Id" } };
        }
    }

    [Obsolete("This class is obsolete. Usage for only apis dependant to P360Controller")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class Ordination
    {
        public string Value { get; set; }
        public bool IsAscending { get; set; }
    }
   
    [Obsolete("This extension is obsolete. Usage for only apis dependant to P360Controller")]
    public static class P360ControllerExtensionMethod
    {
        public static void AddParameter(this IHeaderDictionary headers, HttpRequest request, string key, List<Ordination> ordination, int pageSize, int pageIndex)
        {
            var builder = new StringBuilder();

            builder.Append("?");
            if (ordination != null)
            {
                for (int i = 0; i < ordination.Count; i++)
                {
                    if (builder.Length > 1)
                        builder.Append("&");

                    var value = ordination[i].Value;
                    var isAscending = ordination[i].IsAscending;

                    builder.Append(nameof(ordination)).Append("[").Append(i).Append("]").Append(".")
                           .Append(nameof(value)).Append("=").Append(value)
                           .Append("&")
                           .Append(nameof(ordination)).Append("[").Append(i).Append("]").Append(".")
                           .Append(nameof(isAscending)).Append("=").Append(isAscending.ToString());
                }
            }

            if (builder.Length > 1)
                builder.Append("&");

            var headerParameterLink = builder.Append(nameof(pageSize)).Append("=").Append(pageSize)
                                      .Append("&")
                                      .Append(nameof(pageIndex)).Append("=").Append(pageIndex)
                                      .ToString();

            headerParameterLink = $"{request.Scheme}://{request.Host}{request.Path}{headerParameterLink}";
            headers.Add(key, headerParameterLink);
        }

    }

}
