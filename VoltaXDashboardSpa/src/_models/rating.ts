export interface Rating{
  id : number,
  score : number,
  comment : string,
  userID : number,
  user : any,
  entity : string,
  entityID : number,
  [key: string]: any;
}