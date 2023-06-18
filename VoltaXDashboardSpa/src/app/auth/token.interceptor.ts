import { HttpEvent, HttpInterceptor, HttpRequest, HttpHandler } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { AuthService } from "src/_services/auth.service";



@Injectable()
export class TokenInterceptor implements HttpInterceptor
{
    constructor(public _authService : AuthService){}

    intercept(request : HttpRequest<any>, next : HttpHandler) : Observable<HttpEvent<any>>
    {
        
        request = request.clone({
            setHeaders : {
                Authorization : 'Bearer '+localStorage.getItem("token")+""
            }
        });

        return next.handle(request);
    }
}