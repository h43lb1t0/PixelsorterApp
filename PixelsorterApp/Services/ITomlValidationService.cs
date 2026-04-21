using System;
using System.Collections.Generic;
using System.Text;

namespace PixelsorterApp.Services
{
    public interface ITomlValidationService
    {
        Task<(bool isValid, string errors)> Validate(string conent);
    }
}
