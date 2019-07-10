﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using No9Gallery.Models;
using No9Gallery.Services;

namespace No9Gallery.Controllers
{
    public class PersonInfoController : Controller
    {
        private readonly IPersonInfoService PersonInfoservice;

        public PersonInfoController(IPersonInfoService service)
        {
            PersonInfoservice = service;
        }


        //进入个人主界面
       [Authorize]
        public async Task<IActionResult> Index(string id)
        {
            
            var Info = await PersonInfoservice.GetPersonInfoAsync(id, User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if(Info.Status == "Commom")
            {
                ViewBag.ID = Info.ID;
                ViewBag.VisitID = Info.VisitID;
                ViewBag.IsFollwed = Info.IsFollowed;
                ViewBag.Name = Info.Name;
                //ViewBag.Password = Info.Password;
                // ViewBag.Status = Info.Status;
                ViewBag.Introduction = Info.Introduction;
                ViewBag.HeadportraitURL = Info.HeadPortraitURL;
                ViewBag.Integral = Info.Integral;
                ViewBag.MembershipLevel = Info.membershipLevel;
                ViewBag.WorkNum = Info.Worknum;
                ViewBag.Workids = Info.Workid;
                ViewBag.Workpaths = Info.WorkPath;
                ViewBag.WorkHeadline = Info.WorkNames;
            }
            else
            {
                //导入管理员界面，现在先回主界面
               return Redirect("/Home/Index");

            }
            return View();
        }

        //改变关注状态
        [Authorize]
        public async Task<IActionResult> Follow(string id)
        {
            var visitid = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            string result = await PersonInfoservice.ChangeFollow(visitid, id);
            return Redirect("/PerSonInfo/Index/" + id);
        }

        //进入信息修改界面
        [Authorize]
        public async Task<IActionResult> ReviseView(string id)
        {

            var Info = await PersonInfoservice.GetPersonInfoAsync(id, User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            ViewBag.ID = Info.ID;
            ViewBag.Name = Info.Name;
            ViewBag.Password = Info.Password;
            ViewBag.Status = Info.Status;
            ViewBag.Introduction = Info.Introduction;
            ViewBag.HeadportraitURL = Info.HeadPortraitURL;
            ViewBag.Integral = Info.Integral;
            ViewBag.MembershipLevel = Info.membershipLevel;
            
            return View("InfoRevise");
        }

        //进入作品上传界面
        [Authorize]
        public IActionResult UploadView(string id)
        {
            ViewBag.ID = id;
            return View("UploadView");
        }



        //进入升级界面
        [Authorize]
        public async Task<IActionResult> UpgradeView(string id,string flag,string result)
        {
            
            ViewBag.ID = id;
            var Info = await PersonInfoservice.GetPersonInfoAsync(id, id);
            ViewBag.UserName = Info.Name;
            ViewBag.MembershipLevel = Info.membershipLevel;
            ViewBag.Points = Info.Integral;
            ViewBag.Flag = flag;
            ViewBag.result = result;
       
            return View();
        }

        //进入充值界面
        [Authorize]        
        public async Task<IActionResult>ChargeView(string id)
        {
            
            if (id != User.FindFirst(ClaimTypes.NameIdentifier).Value)
                return Redirect("/PerSonInfo/Index/" +id);
            ViewBag.ID =id;
;            List<Chargelist> ChargeList=await PersonInfoservice.GetChargeListAsync(id);
            int points = await PersonInfoservice.GetPointsAsync(id);
            ViewBag.Points = points;
            return View(ChargeList);
        }

        //提升等级
        [Authorize]
        public async Task<IActionResult>Upgrade(string id,string Level,string points)
        {
            string result;
           
            if(Convert.ToInt32(points) >= 5000)
            {
                 result = await PersonInfoservice.Upgrade(id,Level,points);
            }
            else
            {
                 result = "false";
            }
            return Redirect("/PersonInfo/UpgradeView/?id="+id+"&flag=true&result=" + result);
        }

        //信息修改
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Revise(ReviseInfo reviseinfo)
        {
            reviseinfo.UserID = User.FindFirst(ClaimTypes.NameIdentifier).Value;
           
                string Result = await PersonInfoservice.ReviseInfo(reviseinfo);
            ViewBag.Result = Result;

            return Redirect("/PersonInfo/ReviseView/"+reviseinfo.UserID);
        }

        //头像上传
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadAvatorAsync(List<IFormFile> files)
        {

            string id = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            string Result= await PersonInfoservice.Uploadavatar(id,files);                                    
            return Redirect("/PersonInfo/ReviseView/" +id);
        }

        //提交充值订单
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Chargesubmit(string id)
        {
            //yyyyMMddHHmmss
            var amount =Convert.ToInt32(Request.Form["ChargeAmount"]);
            var chargetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var randomnum = new Random();
            var order_no = DateTime.Now.ToString("yyyyMMddHHmmss") + "_+_" + randomnum.Next(0, 1000).ToString();
            var ConTent = "积分充值";
            int points = await PersonInfoservice.GetPointsAsync(id);
            points = points + amount;
            var result = await PersonInfoservice.ChargeSubmit(order_no, id, amount, ConTent, chargetime,points);
            return Redirect("/PerSonInfo/ChargeView/" + id);
        }

        //作品上传功能
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadWork(string id)
        {
            int i = 0;
            var table = Request.Form;
            IFormFile files =Request.Form.Files[0];
            string Workname = Request.Form["NewWorkName"];
            string WorkType = Request.Form["Worktype"];
            string Introduction = Request.Form["WorkIntroduction"];
            if(files!=null&&Workname!=null&&WorkType!=null&&Introduction!=null)
            {
                string result = await PersonInfoservice.UploadWork(User.FindFirst(ClaimTypes.NameIdentifier).Value,files,Workname,WorkType,Introduction);
            }
            
            return Redirect("/PerSonInfo/UploadView/" + id);
        }

    }
}