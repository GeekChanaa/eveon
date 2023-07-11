import { HttpEvent, HttpInterceptor, HttpRequest, HttpHandler } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable, catchError, throwError } from "rxjs";
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

    // handle your auth error or rethrow
    private handleAuthError() {
        // if (this.auth.isTokenExpired()) {
        // // navigate /delete cookies or whatever
        // console.log('handled error ' );
        // // if you've caught / handled the error, you don't want to rethrow it unless you also want downstream consumers to have to handle it as well.
        // // return Observable.throw(new Error('An error occurred'));
        // }
    }
}