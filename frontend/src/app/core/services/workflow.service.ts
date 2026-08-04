import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateWorkflowConnectionDto, CreateWorkflowNodeDto, MoveWorkflowNodeDto, UpdateWorkflowNodeDto, Workflow, WorkflowConnection, WorkflowExecutionResult, WorkflowNode } from '../models/workflow.model';
@Injectable({ providedIn: 'root' })
export class WorkflowService {
  private readonly api = environment.apiUrl;
  constructor(private readonly http: HttpClient) {}
  getAll(): Observable<Workflow[]> { return this.http.get<Workflow[]>(`${this.api}/api/workflow`); }
  getById(id: number): Observable<Workflow> { return this.http.get<Workflow>(`${this.api}/api/workflow/${id}`); }
  create(name: string, description: string): Observable<Workflow> { return this.http.post<Workflow>(`${this.api}/api/workflow/create`, null, { params: new HttpParams().set('name', name).set('description', description) }); }
  update(workflow: Workflow): Observable<void> { return this.http.put<void>(`${this.api}/api/workflow/${workflow.id}`, workflow); }
  delete(id: number): Observable<void> { return this.http.delete<void>(`${this.api}/api/workflow/${id}`); }
  getNodes(workflowId: number): Observable<WorkflowNode[]> { return this.http.get<WorkflowNode[]>(`${this.api}/api/workflow-nodes/${workflowId}`); }
  addNode(dto: CreateWorkflowNodeDto): Observable<WorkflowNode> { return this.http.post<WorkflowNode>(`${this.api}/api/workflow-nodes`, dto); }
  updateNode(id: number, dto: UpdateWorkflowNodeDto): Observable<void> { return this.http.put<void>(`${this.api}/api/workflow-nodes/${id}`, dto); }
  moveNode(id: number, dto: MoveWorkflowNodeDto): Observable<void> { return this.http.patch<void>(`${this.api}/api/workflow-nodes/${id}/move`, dto); }
  deleteNode(id: number): Observable<void> { return this.http.delete<void>(`${this.api}/api/workflow-nodes/${id}`); }
  getConnections(workflowId: number): Observable<WorkflowConnection[]> { return this.http.get<WorkflowConnection[]>(`${this.api}/api/workflow-connection/${workflowId}`); }
  addConnection(dto: CreateWorkflowConnectionDto): Observable<WorkflowConnection> { return this.http.post<WorkflowConnection>(`${this.api}/api/workflow-connection`, dto); }
  deleteConnection(id: number): Observable<void> { return this.http.delete<void>(`${this.api}/api/workflow-connection/${id}`); }
  execute(workflowId: number, input: string): Observable<WorkflowExecutionResult> { return this.http.post<WorkflowExecutionResult>(`${this.api}/api/workflows/${workflowId}/execute`, { input }); }
}
