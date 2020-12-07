using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RedisAndCore.Models;
using StackExchange.Redis;

namespace RedisAndCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        //KEYS *STU*  SCAN Key//
        //  HGETALL "STUDENT:1" Returns all fields and values of the hash stored at key
        //HDEL "STUDENT:6" "Id"
        //SMEMBERS StudentIndex
        //SET user:id 1000
        // INCR user:id

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;

        }

        public IActionResult Index()
        {
            var db = RedisStore.RedisCache;
            List<Student> studentromRedis = GetStudentsFromRedis();
            return View(studentromRedis);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public List<Student> loaddata()
        {
            var _student = new List<Student>
            {
                new Student { Id = 1, Name = "Moynul", Rollno = "100", Address="Dhaka" },
                new Student { Id = 2, Name = "Biswas", Rollno = "101", Address = "Rajbari" },
                new Student { Id = 3, Name = "Bayzid", Rollno = "102", Address = "Bangladesh" },
                new Student { Id = 4, Name = "Bappy", Rollno = "103", Address = "Rajshai" }
            };

            return _student;
        }

        #region Redis

        public object InsertIntoRedis(List<Student> _student)
        {
            try
            {
                var db = RedisStore.RedisCache;

                foreach (var item in _student)
                {
                    var hashKey = "STUDENT:" + item.Id;

                    db.SetAdd("StudentIndex", hashKey);

                    if (!db.KeyExists(hashKey))
                    {
                        HashEntry[] redisBookHash =
                            {
                                new HashEntry("Id", item.Id),
                                new HashEntry("Name", item.Name),
                                new HashEntry("Rollno", item.Rollno),
                                new HashEntry("Address", item.Address)

                            };
                        db.HashSet(hashKey, redisBookHash);
                    }
                }
                return Ok("Insertation completed");
            }
            catch (Exception ex)
            {
                return BadRequest("Exception occured : " + ex.Message.ToString());
            }
        }

        public List<Student> GetStudentsFromRedis()
        {
            var db = RedisStore.RedisCache;
            RedisValue[] StudentList;
            StudentList = db.SetMembers("StudentIndex");
            var StudentRedisList = StudentList.ToList();

            List<Student> studentList = new List<Student>();
            foreach (var item in StudentRedisList)
            {
                var studentObj = new Student();
                var studentHashID = System.Convert.ToString(item);
                var hashEntries = db.HashGetAll(studentHashID);
                studentObj.Id = (int)hashEntries.Where(entry => entry.Name == "Id").First().Value;
                studentObj.Name = (string)hashEntries.Where(entry => entry.Name == "Name").First().Value;
                studentObj.Rollno = (string)hashEntries.Where(entry => entry.Name == "Rollno").First().Value;
                studentObj.Address = (string)hashEntries.Where(entry => entry.Name == "Address").First().Value;
                studentList.Add(studentObj);
            }
            return studentList;
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var db = RedisStore.RedisCache;
            var user = new Student();
            var studentHashID = System.Convert.ToString("STUDENT:" + id);
            var hashEntries = db.HashGetAll(studentHashID);
            user.Id = (int)hashEntries.Where(entry => entry.Name == "Id").First().Value;
            user.Name = (string)hashEntries.Where(entry => entry.Name == "Name").First().Value;
            user.Rollno = (string)hashEntries.Where(entry => entry.Name == "Rollno").First().Value;
            user.Address = (string)hashEntries.Where(entry => entry.Name == "Address").First().Value;

            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Rollno,Address")] Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var db = RedisStore.RedisCache;
                    var studentHashID = System.Convert.ToString("STUDENT:" + id);
                    HashEntry[] redisBookHash =
                        {
                                new HashEntry("Id", student.Id),
                                new HashEntry("Name", student.Name),
                                new HashEntry("Rollno", student.Rollno),
                                new HashEntry("Address", student.Address)
                            };
                    db.HashSet(studentHashID, redisBookHash);
                }
                catch (Exception ex)
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Rollno,Address")] Student student)
        {
            if (ModelState.IsValid)
            {
                var db = RedisStore.RedisCache;
                var UniqueStudentId = db.StringIncrement("UniqueStudentId", 1);

                RedisValue[] StudentList;
                StudentList = db.SetMembers("StudentIndex");
                var StudentRedisList = StudentList.ToList();
                var studentHashID = System.Convert.ToString("STUDENT:" + UniqueStudentId);
                db.SetAdd("StudentIndex", studentHashID);


                if (!db.KeyExists(studentHashID))
                {
                    HashEntry[] redisBookHash =
                        {
                                new HashEntry("Id", UniqueStudentId),
                                new HashEntry("Name", student.Name),
                                new HashEntry("Rollno", student.Rollno),
                                new HashEntry("Address", student.Address)
                            };
                    db.HashSet(studentHashID, redisBookHash);
                }

                // await _CoreDbProvider.Add(user);
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var db = RedisStore.RedisCache;
            var user = new Student();
            var studentHashID = System.Convert.ToString("STUDENT:" + id);
            var hashEntries = db.HashGetAll(studentHashID);
            user.Id = (int)hashEntries.Where(entry => entry.Name == "Id").First().Value;
            user.Name = (string)hashEntries.Where(entry => entry.Name == "Name").First().Value;
            user.Rollno = (string)hashEntries.Where(entry => entry.Name == "Rollno").First().Value;
            user.Address = (string)hashEntries.Where(entry => entry.Name == "Address").First().Value;

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var db = RedisStore.RedisCache;
            var user = new Student();
            var studentHashID = System.Convert.ToString("STUDENT:" + id);
            var hashEntries = db.HashGetAll(studentHashID);
            user.Id = (int)hashEntries.Where(entry => entry.Name == "Id").First().Value;
            user.Name = (string)hashEntries.Where(entry => entry.Name == "Name").First().Value;
            user.Rollno = (string)hashEntries.Where(entry => entry.Name == "Rollno").First().Value;
            user.Address = (string)hashEntries.Where(entry => entry.Name == "Address").First().Value;

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var db = RedisStore.RedisCache;
            var user = new Student();
            var studentHashID = System.Convert.ToString("STUDENT:" + id);

            db.SetRemove("StudentIndex", studentHashID);

            var entries = db.HashGetAll(studentHashID);
            foreach (var entry in entries)
            {
                var keys = System.Convert.ToString(entry.Name);
                var success = db.HashDelete(studentHashID, keys);
            }

            return RedirectToAction(nameof(Index));
        }

        public HashEntry[] HashGetAll(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            var db = RedisStore.RedisCache;

            return db.HashGetAll(key, flags);
        }
        #endregion

    }
}
