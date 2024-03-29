﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVC_BugTracker.Data;
using MVC_BugTracker.Extensions;
using MVC_BugTracker.Models;
using MVC_BugTracker.Models.Enums;
using MVC_BugTracker.Models.ViewModels;
using MVC_BugTracker.Services.Interfaces;

namespace MVC_BugTracker.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBTProjectService _projectService;
        private readonly IBTCompanyInfoService _infoService;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTRolesService _roleService;

        public ProjectsController(ApplicationDbContext context,
                                  IBTProjectService projectService,
                                  IBTCompanyInfoService infoService,
                                  UserManager<BTUser> userManager, 
                                  IBTRolesService roleService)
        {
            _context = context;
            _projectService = projectService;
            _infoService = infoService;
            _userManager = userManager;
            _roleService = roleService;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Project.Include(p => p.Company).Include(p => p.ProjectPriority);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ALL Projects (By Company)
        public async Task<IActionResult> AllProjects()
        {
            // GET company id
            int companyId = User.Identity.GetCompanyId().Value;

            List<Project> projects = new();

            try
            {
                projects = await _projectService.GetAllProjectsByCompany(companyId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR *** - Error getting all projects - {ex.Message}");
                throw;
            }

            return View(projects);
        }

        // GET: MY Projects
        public async Task<IActionResult> MyProjects()
        {
            // GET company id
            //int companyId = User.Identity.GetCompanyId().Value;

            // GET my user id
            string userId = _userManager.GetUserId(User);

            // SET Model
            List<Project> myProjects = new();

            try
            {
                myProjects = (await _projectService.ListUserProjectsAsync(userId));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR *** - Error getting all tickets - {ex.Message}");
                throw;
            }

            return View(myProjects);
        }




        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Project
                .Include(p => p.Members)
                .Include(p => p.Tickets)
                    .ThenInclude(t => t.TicketStatus)
                .Include(p => p.Company)
                .Include(p => p.ProjectPriority)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Create()
        {
            // ViewData["CompanyId"] = new SelectList(_context.Company, "Id", "Id");
            ViewData["ProjectPriorityId"] = new SelectList(_context.Set<ProjectPriority>(), "Id", "Name");
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Create([Bind("Id,ProjectPriorityId,Name,Description,StartDate,EndDate")] Project project)
        {
            //CompanyId,
            // [TBD - Later] Archived,ArchivedDate,ImageFileName,ImageFileData,ImageContentType

            if (ModelState.IsValid)
            {
                // Get current user
                // BTUser btuser = await _userManager.GetUserAsync(User);

                // Get current company
                int companyId = User.Identity.GetCompanyId().Value;
                project.CompanyId = companyId;

                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details","Projects", new { id=project.Id });
            }
            
            //ViewData["CompanyId"] = new SelectList(_context.Company, "Id", "Id", project.CompanyId);
            //ViewData["ProjectPriorityId"] = new SelectList(_context.Set<ProjectPriority>(), "Id", "Id", project.ProjectPriorityId);

            return RedirectToAction("Create");
        }

        // GET: Projects/Edit/5
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = new Project();
            
            try
            {
                project = await _context.Project.FindAsync(id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            if (project == null)
            {
                return NotFound();
            }
            
            ViewBag.returnUrl = Request.Headers["Referer"].ToString();
            ViewData["ProjectPriorityId"] = new SelectList(_context.Set<ProjectPriority>(), "Id", "Name", project.ProjectPriorityId);
            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Edit(int id, string returnUrl, [Bind("Id,ProjectPriorityId,CompanyId,Name,Description,StartDate,EndDate,Archived,ImageFileName,ImageFileData,ImageContentType")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (project.Archived == true) 
                    {
                        project.ArchivedDate = DateTimeOffset.Now;
                    }
                    else
                    {
                        project.ArchivedDate = null;
                    }

                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                return Redirect(returnUrl);
            }
            
            ViewData["ProjectPriorityId"] = new SelectList(_context.Set<ProjectPriority>(), "Id", "Name", project.ProjectPriorityId);
            return View(project);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> AssignUsers(int id) 
        {
            ProjectMembersViewModel model = new ();

            
            // GET company id
            int companyId = User.Identity.GetCompanyId().Value;

            
            // *** PROJECT ***
            // GET the project
            Project project = new();

            try
            {
                List<Project> projects = await _projectService.GetAllProjectsByCompany(companyId);
                project = projects.FirstOrDefault(p => p.Id == id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR *** - Error getting projects - {ex.Message}");
                throw;
            }

            // ADD to viewmodel
            model.Project = project;


            // *** MULTISELECT ***
            // ?? How to return a null multiselect
            
            // GET users (? not on the project)
            List<BTUser> users = new();

            try
            {
                // REFACTOR - concat devs and submitters

                List<BTUser> developers = await _infoService.GetMembersInRoleAsync(Roles.Developer.ToString(), companyId);
                List<BTUser> submitters = await _infoService.GetMembersInRoleAsync(Roles.Submitter.ToString(), companyId);

                users = developers.Concat(submitters).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR *** - Error getting users not on project - {ex.Message}");
                throw;
            }

            // GET all members from the project
            List<string> members = new();

            try
            {
                if(project?.Members != null)
                {
                    members = project.Members.Select(m => m.Id).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR *** - Error getting members on project - {ex.Message}");
                throw;
            }

            // Add users to multiselect in the VM
            // MS(source, what to select, user sees, optional - show already selected)
            model.Users = new MultiSelectList(users, "Id", "FullName", members);

            // ?? What to do if the model is missing data

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> AssignUsers(ProjectMembersViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.SelectedUsers != null)
                {
                    List<string> memberIds = (await _projectService.GetMembersWithoutPMAsync(model.Project.Id))
                                                    .Select(m => m.Id).ToList(); // select a column

                    foreach (string id in memberIds)
                    {
                        await _projectService.RemoveUserFromProjectAsync(id, model.Project.Id);
                    }

                    foreach (string id in model.SelectedUsers)
                    {
                        await _projectService.AddUserToProjectAsync(id, model.Project.Id);
                    }
                
                    // Goto project details
                    return RedirectToAction("Details", "Projects", new { id = model.Project.Id });
                }
                else
                {
                    // send an error mesage back
                }
            }

            return View(model);
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignPM(int id)
        {
            PMViewModel model = new();


            // GET company id
            int companyId = User.Identity.GetCompanyId().Value;


            // *** PROJECT ***
            // GET the project
            Project project = new();

            try
            {
                List<Project> projects = await _projectService.GetAllProjectsByCompany(companyId);
                project = projects.FirstOrDefault(p => p.Id == id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR *** - Error getting projects - {ex.Message}");
                throw;
            }

            // ADD to viewmodel
            model.Project = project;


            // *** MULTISELECT ***

            // GET users = project manager
            List<BTUser> users = new();

            try
            {
                List<BTUser> pm = await _infoService.GetMembersInRoleAsync(Roles.ProjectManager.ToString(), companyId);
                users = pm;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR *** - Error getting users not on project - {ex.Message}");
                throw;
            }

            // Add to viewmodel???
            // model.Users = users;



            // GET all members from the project
            List<string> members = new();

            try
            {
                if (project?.Members != null)
                {
                    members = project.Members.Select(m => m.Id).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR *** - Error getting members on project - {ex.Message}");
                throw;
            }

            // Add users to multiselect in the VM
            // MS(source, what to select, user sees, optional - show already selected)
            model.Users = new SelectList(users, "Id", "FullName", members);

            BTUser currentPM = await _projectService.GetProjectManagerAsync(project.Id);
            model.SelectedUser = currentPM.Id;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignPM(PMViewModel model)
        {
            if (ModelState.IsValid)
            {

                try
                {
                    await _projectService.AddProjectManagerAsync(model.SelectedUser, model.Project.Id);

                    // Goto project details
                    return RedirectToAction("Details", "Projects", new { id = model.Project.Id });

                }
                catch (Exception)
                {

                    throw;
                }
                
            }

            return View(model);
        }




        private IActionResult RedirectAction(string v1, string v2)
        {
            throw new NotImplementedException();
        }


        // GET: Projects/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Return to Referring Page
            ViewBag.returnUrl = Request.Headers["Referer"].ToString();
            BTUser btUser = await _userManager.GetUserAsync(User);

            #region CheckDemoUser
            // !!! Demo User should not be allowed to delete
            var isDemo = await _roleService.IsUserInRoleAsync(btUser, Roles.DemoUser.ToString());

            if (isDemo)
            {
                TempData["StatusMessage"] = "Error - You do not have access to complete this action.";
                return Redirect(ViewBag.returnUrl);
            }
            #endregion

            var project = await _context.Project
                .Include(p => p.Company)
                .Include(p => p.ProjectPriority)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id, string returnUrl)
        {
            BTUser btUser = await _userManager.GetUserAsync(User);

            #region CheckDemoUser
            // !!! Demo User should not be allowed to delete
            var isDemo = await _roleService.IsUserInRoleAsync(btUser, Roles.DemoUser.ToString());

            if (isDemo)
            {
                TempData["StatusMessage"] = "Error - You do not have access to complete this action.";
                return Redirect(returnUrl);
            }
            #endregion

            var project = await _context.Project.FindAsync(id);
            _context.Project.Remove(project);
            await _context.SaveChangesAsync();
            //return RedirectToAction("AllProjects");
            return Redirect(returnUrl);
        }

        private bool ProjectExists(int id)
        {
            return _context.Project.Any(e => e.Id == id);
        }
    }
}
