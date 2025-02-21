using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using M.Simple.Expand.Test.TestAttributes;

namespace M.Simple.Expand.Test
{
    [System.ComponentModel.Description("D大学教授")]
    public enum PersonEnum
    {
        [Display(Name ="张三",Description ="大学教授")]
        [System.ComponentModel.Description("D大学教授")]
        [MyTitle("国家一级教授")]
        Professor = 1,
        [Display(Name = "李四", Description = "大学老师")]
        [System.ComponentModel.Description("U大学老师")]
        [MyTitle("国家金牌教师")]
        Teacher = 2,
        [Display(Name = "王五", Description = "学生")]
        [System.ComponentModel.Description("大学学生")]
        [MyTitle("流氓学生")]
        Student = 3
    }
}
