using HS.Core;
using HS.Web.Common;
using HS.Web.Middleware;
using HS.Web.Service;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.WebEncoders;
using Microsoft.OpenApi.Models;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();
//builder.Services.AddScoped<DIManager>(); 

builder.Services.AddCors(options =>
{
    options.AddPolicy("apipoilcy", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddMvc().ConfigureApiBehaviorOptions(options =>
{
    // rest api 호출시 닷넷 코어 자체 모델 검증 로직을 사용안함 (값이 null이면 null 그대로 들어옴)
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddRazorPages(option =>
{
    // 토큰 검사 안하기
    option.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute());
});

builder.Services.Configure<WebEncoderOptions>(options =>
{
    // 한글 깨짐 방지
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue; // 크기 제한을 해제하거나 적절한 크기로 조정

    /*
     *  서버쪽 파일 업로드 리밋 제한
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
      <system.webServer>
        <security>
          <requestFiltering>
            <requestLimits maxAllowedContentLength="2147483648" /> // 2G
          </requestFiltering>
        </security>
      </system.webServer>
    </configuration>
     */
});

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "myapi", Version = "v1" });
});

// 사용자 정의 서비스 
//builder.Services.AddHostedService<MongoDBService>();
//builder.Services.AddHostedService<RabbitMQDeclareService>(); // RabbitMQ 큐 및 교환키 이벤트 미리 설정
//builder.Services.AddHostedService<RabbitMQService>();
//builder.Services.AddHostedService<TempReturnJsonHostService>();


// 싱글턴 패턴 의존성 주입
//builder.Services.AddSingleton(sp =>
//{
//    return new RabbitMqService();
//});


//////////////////////////////////////////////////////////////////////////////////






var app = builder.Build();



app.UseMiddleware<CreateClientIDMiddleware>();
app.UseMiddleware<GlogalExceptionMiddleware>();
app.UseDefaultFiles();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "json menual");
});

app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
app.UseHsts();



//app.UseClientUniqueID();

app.UseHttpsRedirection();


//app.UseStaticFiles();

// unity 및 웹 어셈블리 실행을 위한 
app.UseStaticFiles(new StaticFileOptions()
{   
    ServeUnknownFileTypes = true
});


app.UseRouting();
app.UseCors("apipoilcy");

//app.UseCors(option => option.WithOrigins("http://localhost:5150"));
//app.UseCors(options => options.AllowAnyOrigin());


app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});


app.UseAuthorization();

// Configure WebSocket route
app.UseWebSockets();
app.UseMiddleware<WebSocketMiddleware>();

app.MapRazorPages();

app.Run();