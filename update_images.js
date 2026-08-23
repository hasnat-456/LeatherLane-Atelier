const fs = require('fs');

const imageSharpLogic = (urlPrefix, getUniqueNameFn) => `
                    var uniqueFileName = ${getUniqueNameFn};
                    uniqueFileName = System.IO.Path.ChangeExtension(uniqueFileName, ".jpg");
                    var filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var image = await SixLabors.ImageSharp.Image.LoadAsync(fileStream))
                    {
                        if (image.Width > 1200)
                        {
                            image.Mutate(x => x.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions 
                            { 
                                Size = new SixLabors.ImageSharp.Size(1200, 0), 
                                Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max 
                            }));
                        }
                        var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 80 };
                        await image.SaveAsync(filePath, encoder);
                    }
`;

function updateProducts() {
    let code = fs.readFileSync('back/Controllers/ProductsController.cs', 'utf8');
    const oldCode = `var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName.Replace(" ", "_");
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }`;
    const newCode = `var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName.Replace(" ", "_");
                    uniqueFileName = System.IO.Path.ChangeExtension(uniqueFileName, ".jpg");
                    var filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var image = await SixLabors.ImageSharp.Image.LoadAsync(file.OpenReadStream()))
                    {
                        if (image.Width > 1200)
                        {
                            image.Mutate(x => x.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions 
                            { 
                                Size = new SixLabors.ImageSharp.Size(1200, 0), 
                                Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max 
                            }));
                        }
                        var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 80 };
                        await image.SaveAsync(filePath, encoder);
                    }`;
    
    code = code.replace(oldCode, newCode);
    fs.writeFileSync('back/Controllers/ProductsController.cs', code, 'utf8');
}

function updateBlogs() {
    let code = fs.readFileSync('back/Controllers/BlogsController.cs', 'utf8');
    const oldCode = `var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }`;
    const newCode = `var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            uniqueFileName = System.IO.Path.ChangeExtension(uniqueFileName, ".jpg");
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var image = await SixLabors.ImageSharp.Image.LoadAsync(imageFile.OpenReadStream()))
            {
                if (image.Width > 1200)
                {
                    image.Mutate(x => x.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions 
                    { 
                        Size = new SixLabors.ImageSharp.Size(1200, 0), 
                        Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max 
                    }));
                }
                var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 80 };
                await image.SaveAsync(filePath, encoder);
            }`;
    
    code = code.replace(oldCode, newCode);
    fs.writeFileSync('back/Controllers/BlogsController.cs', code, 'utf8');
}

function updateSliders() {
    let code = fs.readFileSync('back/Controllers/AdminApiController.cs', 'utf8');
    const oldCode = `var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(frontPath, uniqueFileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }`;
    const newCode = `var uniqueFileName = Guid.NewGuid().ToString() + ".jpg";
            var filePath = Path.Combine(frontPath, uniqueFileName);
            
            using (var image = await SixLabors.ImageSharp.Image.LoadAsync(imageFile.OpenReadStream()))
            {
                if (image.Width > 1920) // Hero slider can be a bit larger
                {
                    image.Mutate(x => x.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions 
                    { 
                        Size = new SixLabors.ImageSharp.Size(1920, 0), 
                        Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max 
                    }));
                }
                var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 80 };
                await image.SaveAsync(filePath, encoder);
            }`;
    
    code = code.replace(oldCode, newCode);
    fs.writeFileSync('back/Controllers/AdminApiController.cs', code, 'utf8');
}

updateProducts();
updateBlogs();
updateSliders();

console.log("Updated controllers for ImageSharp compression!");
